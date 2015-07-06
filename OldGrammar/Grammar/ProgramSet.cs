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
            return new GpfProgram(GetRuleProgram(), GetFilterProgram(), MemoryCoordinator.GetProgramMemory());
        }

        public List<char> GetRuleProgram()
        {
            List<char> program = new List<char>();

            //indicate number of layers
            program.Add((char)RuleProgram.Count);

            foreach (var set in RuleProgram)
            {
                //write layer test header
                program.Add((char)set.Records.Count);

                //specify setup for each protocol in layer
                foreach (var record in set.Records)
                {
                    program.Add((char)record.Protocol.Identifier);      //which protocol
                    program.Add((char)record.Protocol.DefaultLength);   //default length of the protocol
                    program.Add((char)record.Protocol.RuleIndex);       //Rule write index (-1 for none)
                }

                foreach (var bucket in set.Cache.Buckets)
                {
                    //write cache load instruction

                    //iterate through field sets, writing records relevant to the cache bucket
                    CacheBucket tmp = bucket;
                    foreach (var protocol in set.Records)
                    {
                        var relevant = protocol.MasterRecords.Where(r => tmp.ContainedFieldRecords.Contains(r)).ToList();
                        if (relevant.Any())
                        {
                            //indicate the number of fields in the load
                            program.Add((char)relevant.Count);
                            relevant.Sort();

                            foreach (var field in relevant)
                            {
                                //FIELD HANDLING
                                //--------------

                                //provide field range
                                program.Add((char)field.Range.StartOffset);
                                program.Add((char)field.Range.Length);

                                //indicate the result index (-1 if none)
                                program.Add((char)field.ResultIndex);
                                //indicate the number of filters to be performed

                                //FILTER HANDLING
                                //--------------

                                program.Add((char)field.Filters.Count);

                                //perform filters
                                foreach (var filter in field.Filters)
                                {
                                    //indicate the protocol identifier of the filter (-1 for non switch filters)
                                    program.Add((char)filter.SwitchValue);
                                    //indicate rule index
                                    program.Add((char)filter.RuleIndex);
                                    //specify comparison
                                    program.Add((char)filter.Comparison);
                                    //specify target
                                    program.Add((char)filter.LookupIndex);
                                }

                                //indicate if it is a switch field - triggers copy of the switch reg to next protocol reg if true
                                program.Add(field.IsSwitch ? (char)1 : (char)0);

                                //STATEMENT HANDLING
                                //--------------

                                //process statement expression if any
                                if (field.StatementTransactions != null)
                                {
                                    program.AddRange(field.StatementTransactions.ToCharCode());
                                }
                                else program.Add((char)0);
                            }
                        }
                    }
                }
            }
            return program;
        }

        public List<char> GetFilterProgram()
        {
            List<char> program = new List<char>();

            program.Add((char)Filters.Count);

            foreach (var filter in Filters)
            {
                program.AddRange(filter.ToCharCode());
            }

            return program;
        }
    }


    public class ProtocolTest : IEquatable<ProtocolTest>
    {
        public Protocol Protocol { get; private set; }
        public int BoolRuleIndex { get; private set; }

        public ProtocolTest(Protocol protocol)
        {
            this.Protocol = protocol;
            BoolRuleIndex = protocol.RuleIndex;
        }

        public bool Equals(ProtocolTest other)
        {
            return Protocol.Equals(other.Protocol);
        }
    }

    public class ProtocolSet
    {
        public List<FieldSet> Records;
        public List<ProtocolTest> ProtocolTests;

        public CacheChain Cache;

        public ProtocolSet(ProgramLayer layer)
        {
            Records = new List<FieldSet>();
            ProtocolTests = new List<ProtocolTest>();

            foreach (var protocol in layer.Protocols)
            {
                //protocol tests are performed prior to and separately from field tests, as most protocol tests wont require field tests
                if (protocol.IsRule)
                {
                    ProtocolTests.Add(new ProtocolTest(protocol));
                }
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

            var tmp = new List<CacheBucket>(){new CacheBucket(records[0])};

            int curr = 0;
            foreach (var record in records)
            {
                if (!tmp[curr].TryAddAdjacent(record))
                {
                    tmp.Add(new CacheBucket(record));
                    curr++;
                }
            }

            tmp.Sort();

            foreach (var bucket in tmp.Where(bucket => !Buckets.Any(existing => existing.TryMerge(bucket))))
            {
                Buckets.Add(bucket);
            }
        }
    }

    public class CacheBucket : IComparable<CacheBucket>
    {
        public int StartOffset { get; private set; }
        public int EndOffset { get { return StartOffset + Ints; } }
        public int Ints { get; private set; }

        public List<FieldRecord> ContainedFieldRecords { get; private set; }

        public CacheBucket(FieldRecord record)
        {
            StartOffset = record.Range.StartOffset / 32;
            Ints = record.Range.ReadWidth;
            ContainedFieldRecords = new List<FieldRecord>() {record};
        }

        public bool TryAddAdjacent(FieldRecord record)
        {
            var offset = record.Range.StartOffset / 32;
            var range = record.Range.ReadWidth;
            
            //if the record is contained in the same integer load
            if (offset == StartOffset)
            {
                Ints = Math.Max(Ints, range);
                ContainedFieldRecords.Add(record);
                return true;
            }
            //false if the record is not directly adjacent
            if (offset != StartOffset + 1) return false;
            Ints = Math.Max(Ints, 1 + range); ; //directly adjacent so increase size
            ContainedFieldRecords.Add(record);
            return true;
        }

        public bool TryMerge(CacheBucket bucket)
        {
            var size = Math.Max(EndOffset, bucket.EndOffset) - Math.Min(StartOffset, bucket.StartOffset);
            if (size > 4) return false;

            StartOffset = Math.Min(StartOffset, bucket.StartOffset);
            Ints = size;
            ContainedFieldRecords.AddRange(bucket.ContainedFieldRecords);
            return true;
        }

        //larger first, then earliest first
        public int CompareTo(CacheBucket other)
        {
            if (Ints == other.Ints)
            {
                return StartOffset.CompareTo(other.StartOffset);
            }
            if (Ints < other.Ints) return 1;
            return -1;
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
                if (f == protocol.Switch.Target)
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

            var k = 1; //0 represents no match
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
            LookupIndex = filter.LookupIndex;
            RuleIndex = filter.RuleIndex;
            SwitchValue = -1;

            foreach (var switchCase in filter.Parent.Parent.Switch.Cases.Where(switchCase => switchCase.Filter == Name))
            {
                SwitchValue = ProtocolLibrary.GetProtocol(switchCase.Protocol).Identifier;
            }
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

        public List<char> ToCharCode()
        {
            return Transactions.ToCharCode();
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