using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using NetMQ;
using ViewTimeline.Graphs;

namespace ViewTimeline.FieldSet
{
    class FieldSetData 
    {
        private const int ReadBufferSize = 8 * 1024 * 1024;
        private const int IntsPerBuffer = ReadBufferSize / 4;

        private string _fieldFile;
        private List<FieldSetRecord> records;
        private List<FieldSetRecord> valid_records; 
        private long _processedCount;

        public bool Filled { get; private set; }

        public FieldSetData()
        {
            Filled = false;
        }
        
        public long Records { get; private set; }

        public string Capture { get; private set; }

        public async Task Initialise(string fieldFile)
        {
            Filled = false;

            _fieldFile = fieldFile;


            //NetMQSocket socket = CanvasManager.ServerSocket.GetSocket(ServerSocketType.Reduce);

            _processedCount = 0;

            var start = DateTime.Now.Ticks;
            
            using (var file = new FileStream(_fieldFile, FileMode.Open, FileAccess.Read))
            {
                var buffer = new byte[272];
                file.Seek(0, SeekOrigin.Begin);
                file.Read(buffer, 0, 272);
                Records = Convert.ToInt64(BitConverter.ToInt64(buffer, 8));
                Capture = BitConverter.ToString(buffer, 144, 128);

                long validCount;
                string metaFile = fieldFile.Substring(0, fieldFile.LastIndexOf('.') + 1) + "gpf_field_meta";
                if (File.Exists(metaFile))
                {
                    records = Load(metaFile, Records);
                    valid_records = records.Where(r => r.Value != 0).ToList();
                    validCount = valid_records.Sum(v => v.Count);
                    for (int index = 0; index < valid_records.Count; index++)
                    {
                        valid_records[index].SetValidTotal(validCount);
                    }
                    Filled = true;
                    return;
                }

                Dictionary<int, long> valueCountPairs = new Dictionary<int, long>();
                var iterations = Convert.ToInt32(Math.Ceiling((double) Records / IntsPerBuffer));

                //socket.Send(BitConverter.GetBytes(ite
                buffer = new byte[ReadBufferSize];
                for (var k = 0; k < iterations; k++)
                {
                    var result = file.ReadAsync(buffer, 0, ReadBufferSize);
                    
                    await result;
                    //socket.Send(buffer, result.Result);

                    AddRange(buffer, result.Result / sizeof (int), valueCountPairs);
                }

                records = valueCountPairs.Select(p => new FieldSetRecord(p.Key, p.Value, Records)).ToList();
                valid_records = records.Where(r => r.Value != 0).ToList();
                validCount = valid_records.Sum(v => v.Count);
                foreach (var val in valid_records)
                {
                    val.SetValidTotal(validCount);
                }

               // Store(records, metaFile);

                //for (var k = 0; k < iterations; k++)
                //{
                //    var keysByte = socket.Receive();
                //    var valuesByte = socket.Receive();

                //    await AddRange(keysByte, valuesByte, keysByte.Length / sizeof (int));
                //}
            }
            var end = DateTime.Now.Ticks;

            var time = TimeSpan.FromTicks(end - start);
            MessageBox.Show("Total Time: " + time.TotalSeconds + "\nPackets/s: " + (Records / time.TotalSeconds),
                "Performance");

            //CanvasManager.ServerSocket.ReturnSocket(ref socket);
            Filled = true;
        }

        private async Task AddRange(byte[] keys, byte[] values, int count, Dictionary<int, long> dict)
        {
            for (int k = 0; k < count; k++)
            {
                int key = BitConverter.ToInt32(keys, k * sizeof (int));
                int value = BitConverter.ToInt32(values, k * sizeof (int));

                if (dict.ContainsKey(key)) dict[key] += value;
                else dict.Add(key, value);
                _processedCount += value;
                if (_processedCount == Records) return;
            }
        }

        private void AddRange(byte[] keys, int count, Dictionary<int, long> dict)
        {
            for (int k = 0; k < count; k++)
            {
                int key = BitConverter.ToInt32(keys, k * sizeof(int));
                if (dict.ContainsKey(key)) dict[key]++;
                else dict.Add(key, 1);
                _processedCount++;
                if (_processedCount == Records) return;
            }
        }
        
        public float Completion
        {
            get { return (float) _processedCount / Records; }
        }

        public List<FieldSetRecord> GetRecords
        {
            get
            {
                if (!Filled) throw new Exception("Records cannot be retreived before the Set is filled with Initilise");
                return records;
            }
        }
        public List<FieldSetRecord> GetValidRecords
        {
            get
            {
                if (!Filled) throw new Exception("Records cannot be retreived before the Set is filled with Initilise");
                return valid_records;
            }
        }

        public static void Store(List<FieldSetRecord> data, string filename)
        {
            using (var s = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                List<byte> bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes(data.Count));
                foreach (var record in data)
                {
                    bytes.AddRange(BitConverter.GetBytes(record.Value));
                    bytes.AddRange(BitConverter.GetBytes(record.Count));
                }

                byte[] buffer = bytes.ToArray();
                s.Write(buffer, 0, buffer.Length);
            }
        }

        public static List<FieldSetRecord> Load(string filename, long total)
        {
            List<FieldSetRecord> data = new List<FieldSetRecord>();
            using (var s = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[16];
                s.Read(buffer, 0, 4);
                int count = BitConverter.ToInt32(buffer, 0);

                for (int k = 0; k < count; k++)
                {
                    s.Read(buffer, 0, 12);
                    data.Add(new FieldSetRecord(BitConverter.ToInt32(buffer, 0), BitConverter.ToInt64(buffer, 4), total));
                }
            }
            return data;
        }

    }

    public class FieldSetRecord : IComparable<FieldSetRecord>
    {
        public int Value { get; private set; }
        public double TrafficPercent { get; private set; }
        public long Count { get; private set; }
        public double ValidTrafficPercent { get; set; }

        public FieldSetRecord(int value, long count, long total) //: this()
        {
            Value = value;
            Count = count;

            TrafficPercent = count * 100.0 / total;
            ValidTrafficPercent = 0;
        }

        public void SetValidTotal(long validTotal)
        {
            ValidTrafficPercent = Count * 100.0 / validTotal;
        }

        //sort descending
        public int CompareTo(FieldSetRecord other)
        {
            return -Count.CompareTo(other.Count);
        }
    }

}
