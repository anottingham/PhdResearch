using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Grammar
{
    public class GpfProgram
    {
        private const int slackBytes = 8;

        public List<byte> RuleProgram { get; private set; }
        public List<byte> FilterProgram { get; private set; }

        public ProgramMemory ProgramMemory { get; private set; }

        public int DataStart { get; private set; }
        public int DataLength { get; private set; }

        public int LayerCount { get; private set; }
        public int Root { get; private set; }

        public List<string> FilterNames { get; private set; }
        public List<string> IntegerNames { get; private set; }

        public void ToFile(string filename)
        {
            List<byte> fileBytes = new List<byte>();
            fileBytes.AddRange(StaticMemory);
            fileBytes.AddRange(BitConverter.GetBytes(RuleProgram.Count));
            fileBytes.AddRange(RuleProgram);
            fileBytes.AddRange(BitConverter.GetBytes(FilterProgram.Count));
            fileBytes.AddRange(FilterProgram);

            var lookup = LookupMemory;
            fileBytes.AddRange(BitConverter.GetBytes(lookup.Count));
            fileBytes.AddRange(lookup);

            for (int k = 0; k < FilterNames.Count; k++)
            {
                fileBytes.AddRange(BitConverter.GetBytes(FilterNames[k].Length));
                fileBytes.AddRange(Encoding.ASCII.GetBytes(FilterNames[k]));
            }

            for (int k = 0; k < IntegerNames.Count; k++)
            {
                fileBytes.AddRange(BitConverter.GetBytes(IntegerNames[k].Length));
                fileBytes.AddRange(Encoding.ASCII.GetBytes(IntegerNames[k]));
            }

            FileStream fs = new FileStream(filename, FileMode.Create);
            fs.Write(fileBytes.ToArray(), 0, fileBytes.Count);
            fs.Close();

        }

        public GpfProgram(ProgramSet set)
        {
            RuleProgram = set.GetRuleProgram();
            FilterProgram = set.GetFilterProgram();
            ProgramMemory = MemoryCoordinator.GetProgramMemory();

            DataStart = set.RecordStartByte;
            DataLength = set.RecordLengthByte + slackBytes;
            DataLength += (DataLength % 4 != 0 ? (4 - DataStart % 4) : 0);
            LayerCount = set.RuleProgram.Count;

            Root = ProtocolLibrary.GetRoot().Identifier;
            //update root start / end
            //ensure records are multiples of 4 bytes
            if (DataLength < 16) DataLength = 16;
            if (DataLength % 4 != 0) DataLength += 4 - (DataLength % 4);

            FilterNames = set.Filters.Select(f => f.ID).ToList();
            IntegerNames = set.Reads.Select(f => f.ID).ToList();
        }

        public List<byte> StaticMemory
        {
            get
            {
                var tmp = new List<byte>();
                tmp.AddRange(BitConverter.GetBytes(DataStart));
                tmp.AddRange(BitConverter.GetBytes(DataLength));
                tmp.AddRange(BitConverter.GetBytes(ProgramMemory.RuleCount));
                tmp.AddRange(BitConverter.GetBytes(ProgramMemory.FilterCount));
                tmp.AddRange(BitConverter.GetBytes(ProgramMemory.IntCount));
                tmp.AddRange(BitConverter.GetBytes(ProgramMemory.LookupMemory.Count));
                tmp.AddRange(BitConverter.GetBytes(LayerCount));
                tmp.AddRange(BitConverter.GetBytes(Root));

                return tmp;
            }
        }

        public List<byte> LookupMemory
        {
            get
            {
                var tmp = new List<byte>();
                foreach (var s in ProgramMemory.LookupMemory)
                {
                    tmp.AddRange(BitConverter.GetBytes(s));
                }
                return tmp;
            }
        }
    }
}
