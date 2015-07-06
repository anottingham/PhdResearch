using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Remoting.Messaging;

namespace Grammar
{
    public class ProgramSet
    {
        public List<ProtocolSet> RuleProgram { get; private set; }
        public List<KernelFilterRecord> Filters { get; private set; }
        public List<KernelReadRecord> Reads { get; private set; }

        public ProgramSet()
        {
            RuleProgram = new List<ProtocolSet>();
            Filters = new List<KernelFilterRecord>();
            Reads = new List<KernelReadRecord>();
        }


        public void Generate()
        {
            foreach (var layer in GpfProgramCompiler.Layers)
            {
                RuleProgram.Add(new ProtocolSet(layer));
            }

            foreach (var kernel in ProtocolLibrary.Kernels.Kernels)
            {
                switch (kernel.Type)
                {
                    case KernelType.Filter:
                        var filter = (FilterKernel) kernel;
                        Filters.Add(new KernelFilterRecord(filter));
                        break;
                    case KernelType.Field:
                        var field = (FieldKernel) kernel;
                        Reads.Add(new KernelReadRecord(field));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }


        public GpfProgram GetProgram()
        {
            var length = 0;
            foreach (var layer in RuleProgram)
            {
                var protocols = layer.Records.Select(s => s.Protocol);

                var lengths = new List<int>();
                foreach (var protocol in protocols)
                {
                    //if protocol has dependents then whole default length must be included
                    if (protocol.Dependants.Count > 0)
                    {
                        lengths.Add(protocol.DefaultLength);
                    }
                    else
                    {
                        //terminal protocol - dont need full length
                        int max = int.MinValue;
                        foreach (Field w in protocol.Fields)
                        {
                            if (w.IsResult || w.Filters.Count > 0) max = Math.Max(max, w.Range.EndOffset);
                        }
                        lengths.Add(max);
                    }
                }
                length += lengths.Max();
            }

            length = length - RuleProgram[0].Cache.Buckets[0].BitOffset;
            length = length / 8 + (length % 8 == 0 ? 0 : 1);

            //update root to shift off unused start bytes
            var start = RuleProgram[0].Cache.Buckets[0].ByteOffset;
            ProtocolLibrary.RealignRoot(start);
            RuleProgram[0].Cache.Buckets.ForEach(b => b.BitOffset -= start * 8);

            RecordStartByte = start;
            RecordLengthByte = length;
            return new GpfProgram(this);
        }

        public List<byte> GetRuleProgram()
        {
            var program = new List<byte>(); //layers set elsewhere
            
            foreach (var set in RuleProgram)
            {
                //write layer test header
                program.Add((byte)set.Records.Count);

                //specify setup for each protocol in layer
                foreach (var record in set.Records)
                {
                    program.Add((byte)record.Protocol.Identifier);      //which protocol
                    program.Add((byte)record.Protocol.DefaultLengthBytes);   //default length of the protocol
                }

                //add placeholder for skip offset
                program.Add(0);
                int idx = program.Count - 1;

                //number of cache buchets
                program.Add((byte)set.Cache.Buckets.Count);

                foreach (var bucket in set.Cache.Buckets)
                {
                    //write cache load instruction
                    program.Add((byte)bucket.ByteOffset);
                    program.Add((byte)set.Records.Count);
                    //iterate through field sets, writing records relevant to the cache bucket
                    foreach (var protocol in set.Records)
                    {
                        var relevant = protocol.MasterRecords.Where(r => bucket.ContainedFieldRecords.Contains(r)).ToList();
                        if (relevant.Any())
                        {
                            
                            program.Add((byte)protocol.Protocol.Identifier);
                            program.Add(0); //placeholder for next index
                            int index = program.Count - 1;

                            //indicate the number of fields in the load
                            program.Add((byte)relevant.Count);
                            relevant.Sort();

                            foreach (var field in relevant)
                            {
                                //FIELD HANDLING
                                //--------------

                                //provide field range
                                program.Add((byte)field.CacheAdjustedOffset);
                                program.Add((byte)field.Range.Length);

                                //indicate the result index (-1 if none)
                                program.Add((byte)field.ResultIndex);
                                //indicate the number of filters to be performed

                                //FILTER HANDLING
                                //--------------

                                program.Add((byte)field.Filters.Count);

                                //perform filters
                                foreach (var filter in field.Filters)
                                {
                                    //specify comparison
                                    program.Add((byte) filter.Comparison);
                                    //specify target
                                    program.Add((byte) filter.LookupIndex);

                                    //indicate the protocol identifier of the filter (-1 / 0xff for non switch filters)
                                    program.Add((byte) filter.SwitchValue);
                                    //indicate rule index
                                    program.Add((byte) filter.RuleIndex);
                                }

                                //STATEMENT HANDLING
                                //--------------

                                //process statement expression if any
                                if (field.StatementTransactions != null)
                                {
                                    program.AddRange(field.StatementTransactions.ToByteCode());
                                }
                                else program.Add((byte)0);
                            }
                            int next = program.Count - index + 1; //account for vm offset (value occurs at local program index 1 not 0)
                            program[index] = (byte)next; //set jump info
                        }
                    }
                }
                int nxt = program.Count - idx;
                program[idx] = (byte)nxt; //set jump info
            }
            return program;
        }

        public List<byte> GetFilterProgram()
        {
            List<byte> program = new List<byte>();
            
            foreach (var filter in Filters)
            {
                program.AddRange(filter.ToByteCode());
            }

            return program;
        }

        public int RecordStartByte { get; private set; }

        public int RecordLengthByte { get; private set; }
    }

    public class ProtocolSet
    {
        public List<FieldSet> Records;

        public CacheChain Cache;

        public ProtocolSet(ProgramLayer layer)
        {
            Records = new List<FieldSet>();

            foreach (var protocol in layer.Protocols)
            {
                Records.Add(new FieldSet(protocol));
            }

            Cache = new CacheChain(Records.SelectMany(r => r.MasterRecords).ToList());
            
        }
    }

    public class CacheChain
    {
        public List<CacheBucket> Buckets { get; private set; }

        public CacheChain(List<FieldRecord> records)
        {
            Buckets = new List<CacheBucket>();
            records.Sort();

            var tmp = new List<CacheBucket>();

            foreach (var record in records)
            {
                tmp.Add(new CacheBucket(record));
            }


            foreach (var bucket in tmp.Where(bucket => !Buckets.Any(existing => existing.TryMerge(bucket))))
            {
                Buckets.Add(bucket);
            }

            foreach (var bucket in Buckets)
            {
                foreach (var field in bucket.ContainedFieldRecords)
                {
                    field.SetCacheBaseOffset(bucket.ByteOffset);
                }
            }
        }
    }

    public class CacheBucket : IComparable<CacheBucket>
    {
        public int BitOffset { get; set; }
        public int BitLength { get; private set; }
        public int EndOffset { get { return BitOffset + BitLength; } }

        public int ByteOffset { get { return BitOffset / 8; } }

        public List<FieldRecord> ContainedFieldRecords { get; private set; }

        public CacheBucket(FieldRecord record)
        {
            BitOffset = record.Range.StartOffset;
            BitLength = record.Range.Length;
            ContainedFieldRecords = new List<FieldRecord>() {record};

        }

        public bool TryMerge(CacheBucket other)
        {
            int start = Math.Min(BitOffset, other.BitOffset);
            int end = Math.Max(EndOffset, other.EndOffset);
            int len = end - start;

            //cache load is limited to 3 ints = 12 bytes
            if  (len > 96) return false;

            BitOffset = start;
            BitLength = len;
            ContainedFieldRecords.AddRange(other.ContainedFieldRecords);

            return true;
        }
        //larger first, then earliest first
        public int CompareTo(CacheBucket other)
        {
            if (BitLength == other.BitLength) return BitOffset.CompareTo(other.BitOffset);
            else return BitLength.CompareTo(other.BitLength);
        }

        public List<byte> ToByteCode()
        {
            return new List<byte> {(byte) ByteOffset};
        }
    }
    /// <summary>
    /// An ordered collection of fields for a specific protocol in a layer
    /// </summary>
    public class FieldSet
    {
        public Protocol Protocol { get; private set; }
        public List<FieldRecord> MasterRecords { get; private set; }
        
        public FieldSet(Protocol protocol)
        {
            MasterRecords = new List<FieldRecord>();
            Protocol = protocol;

            foreach (var f in Protocol.Fields)
            {
                var field = new FieldRecord(f);
                
                //if f is the target of the protocols switch operation
                if (protocol.Switch != null && f == protocol.Switch.Target)
                {
                    field.IsSwitch = true;
                }

                MasterRecords.Add(field);
            }

            MasterRecords.Sort();
        }
    }

    public class FieldRecord : IComparable<FieldRecord>, IEquatable<FieldRecord>
    {
        public FieldRange Range { get; private set; }
        public ExpressionTransactionSet StatementTransactions { get; private set; }
        public List<FilterRecord> Filters;
        public Protocol Protocol { get; private set; }

        public int CacheAdjustedOffset { get; private set; }

        public void SetCacheBaseOffset(int byteOffset)
        {
            CacheAdjustedOffset = Range.StartOffset - byteOffset * 8;
            if (CacheAdjustedOffset < 0) throw new ArgumentOutOfRangeException("the specified cache offset does not preceed the field start offset");
        }

        /// <summary>
        /// Contains protocol specific operations for storing integer results
        /// </summary>
        public int ResultIndex { get; private set; }

        /// <summary>
        /// Indicates whether the field is the target of the protocols switch statement
        /// </summary>
        public bool IsSwitch { get; set; }

        public FieldRecord(Field field)
        {
            Protocol = field.Parent;
            Filters = new List<FilterRecord>();
            Range = field.Range;

            IsSwitch = false;
            ResultIndex = field.ResultIndex;
            if (ResultIndex == -1) ResultIndex = 0xFF;

            Filters = field.Filters.Select(f => new FilterRecord(f)).ToList();

            if (field.Statement != null)
            {
                //all statements write to $length register (sys_reg[0])
                StatementTransactions = field.Statement.Expression.GenerateExpressionTransactions(new ExpressionWriteTransaction(ExprOutLoc.SystemReg, 0));
            }
        }

        public int CompareTo(FieldRecord other)
        {
            return Range.CompareTo(other.Range);
        }

        public int GetFieldFilterId(string id)
        {
            return Filters.Find(f => f.Name == id).SwitchValue;
        }

        public bool Equals(FieldRecord other)
        {
            return Range.Equals(other.Range);
        }

    }

    public class FilterRecord
    {
        public Comparison Comparison { get; private set; }
        /// <summary>
        /// The index in the lookup table which contains the comparison value
        /// </summary>
        public int LookupIndex { get; private set; }

        /// <summary>
        /// The string name of the filter
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The index of the protocol to set if the filter passes
        /// </summary>
        public int SwitchValue { get; private set; }

        /// <summary>
        /// The index in rule memory where the filter result should be stored
        /// </summary>
        public int RuleIndex { get; private set; }

        public FilterRecord(FieldFilter filter)
        {
            Name = filter.ID;
            Comparison = filter.Comparison;
            LookupIndex = MemoryCoordinator.RegisterStaticInteger(filter.Value);
            RuleIndex = filter.RuleIndex;
            SwitchValue = 0;

            var protocolSwitch = filter.Parent.Parent.Switch;
            if (protocolSwitch == null) return;

            //need to check for empty protocols, and give these a switch of 0
            if (protocolSwitch.Cases.Select(c => c.Filter).ToList().Contains(Name))
                SwitchValue = ProtocolLibrary.GetProtocol(protocolSwitch.Cases.Find(switchCase => switchCase.Filter == Name).Protocol).Identifier;
            
        }
    }

    
    public class KernelFilterRecord
    {
        public string ID { get; private set; }
        public int BoolResultIndex { get; private set; }

        public PredicateTransactionSet Transactions { get; private set; }

        public KernelFilterRecord(FilterKernel kernel)
        {
            ID = kernel.ID;
            BoolResultIndex = kernel.Filter.WriteIndex;
            Transactions = new PredicateTransactionSet(kernel.Filter);
        }

        public List<byte> ToByteCode()
        {
            return Transactions.ToByteCode();
        }
    }


    /// <summary>
    /// This class is only a data container at this point. Serves no purpose at this time.
    /// </summary>
    public class KernelReadRecord
    {
        public string ID { get; private set; }
        public int IntResultIndex { get; private set; }

        public KernelReadRecord(FieldKernel kernel)
        {
            ID = kernel.ID;
            IntResultIndex = ProtocolLibrary.GetField(kernel.Protocol, kernel.Field).ResultIndex;
        }
    }
}