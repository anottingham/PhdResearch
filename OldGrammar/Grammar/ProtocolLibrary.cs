using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Grammar
{


    public static class ProtocolLibrary
    {
        public static Dictionary<string, Protocol> Protocols { get; private set; }
        private static Protocol head;
        public static ProcessKernel Kernels { get; private set; }

        static ProtocolLibrary()
        {
            head = null;
            Kernels = new ProcessKernel();
        }

        public static void Clear()
        {
            MemoryCoordinator.Reset();
            GpfProgramCompiler.Clear();
            Protocols.Clear();
            Kernels.Clear();
            head = null;
        }

        public static void AddProtocol(Protocol protocol)
        {
            if (head == null) head = protocol;
            Protocols.Add(protocol.Name, protocol);
        }

        public static Protocol GetRoot()
        {
            return head;
        }

        public static Protocol GetProtocol(string id)
        {
            if( Protocols.ContainsKey(id)) return Protocols[id];
            throw new Exception("Error: Protocol " + id + " does not exist.");
        }


        public static List<Protocol> GetProtocols(IEnumerable<ProtocolAtom> protocols)
        {
            return protocols.Select(p => GetProtocol(p.Protocol)).ToList();
        }

        public static Field GetField(string protocolId, string fieldId)
        {
            var protocol = GetProtocol(protocolId);

            if (protocol.Fields.Exists(x => x.ID == fieldId)) return protocol.Fields.Find(x => x.ID == fieldId);
            throw new Exception("Error: FieldID " + protocolId + "." + fieldId + " does not exist.");

        }
        
        public static FieldFilter GetFieldFilter(string protocolId, string fieldId, string filterId)
        {
            var field = GetField(protocolId, fieldId);
            if (field.Filters.Exists(x => x.ID == filterId)) return field.Filters.Find(x => x.ID == filterId);
            throw new Exception("Error: FieldID Filter " + protocolId + "." + fieldId + "." + filterId + " does not exist.");
        }

        public static List<FieldFilter> GetFieldFilters(IEnumerable<FilterAtom> filters)
        {
            return filters.Select(f => GetFieldFilter(f.Protocol, f.Field, f.Filter)).ToList();
        }

        public static void GenerateProgram()
        {
            Kernels.BuildKernelProgram();
            //connect all protocols to one another - builds dependant and dependency lists
            head.Connect();

            //remove all protocols which do not connect the root to any of the target fields
            var remove = Protocols.Values.Where(protocol => !protocol.Required(Kernels.Targets.ReleventFields, head)).ToList();
            //remaining protocols are those required to process the supplied targets
            var list = Protocols.Values.Except(remove).ToList();
            list.Sort();

            //trim redundant refs from the remaining protocols and connect switch to switch field
            var k = 0;
            foreach (var p in list)
            {
                p.RemoveRedundancies(Kernels.Targets, list);
                p.Switch.ResolveField();
                p.Identifier = k++;
            }

            GpfProgramCompiler.Compile(list);

        }


    }


    public class Protocol : IEquatable<Protocol>, IComparable<Protocol>
    {
        public List<Field> Fields { get; private set; }
        public int DefaultLength { get; private set; }
        public string Name { get; private set; }
        public Switch Switch { get; set; }

        public int RuleIndex { get; private set; }
        public int StoreAsRule()
        {
            if (RuleIndex == -1)
            {
                RuleIndex = MemoryCoordinator.GetBoolRuleIndex();
            }
            return RuleIndex;
        }
        public bool IsRule { get { return RuleIndex >= 0; }}

        public List<Protocol> Parents; 
        public List<Protocol> Children;

        public HashSet<Protocol> Dependencies;
        public HashSet<Protocol> Dependants;
    
        public int Identifier { get; set; }

        public Protocol(string name)
        {
            this.Name = name;
            DefaultLength = -1;
            Fields = new List<Field>();
            Switch = null;
            RuleIndex = -1;

            Parents = new List<Protocol>();
            Children = new List<Protocol>();

            Dependencies = new HashSet<Protocol>();
            Dependants = new HashSet<Protocol>();
        }

        public void AddField(Field field)
        {
            Fields.Add(field);
        }

        public void AddParent(Protocol parent)
        {
            if (Parents.Contains(parent)) {
                MessageBox.Show("Duplicate Parent Id " + parent.Name + " in Protocol " + Name + ".");   //unique values only -maybe error?
                return;
            }
            Parents.Add(parent);
            Dependencies.Add(parent);
            Dependencies.UnionWith(parent.Dependencies);
        }

        public bool Equals(Protocol other)
        {
            return Name == other.Name;
        }

        public void Connect()
        {
            DefaultLength = Fields.Select(f => f.Range.EndOffset).ToList().Max();

            foreach (var protocol in Switch.Cases.Select(tmp => ProtocolLibrary.GetProtocol(tmp.Protocol)).Where(protocol => protocol != null))
            {
                protocol.AddParent(this);
                Children.Add(protocol);

                protocol.Connect();

                Dependants.Add(protocol);
                Dependants.UnionWith(protocol.Dependants);
            }
        }

        /// <summary>
        /// Removes protocols and fields
        /// </summary>
        /// <param name="targets"></param>
        /// <param name="relevantProtocols">a list containing all relevant protocols (including prerequisits)</param>
        public void RemoveRedundancies(Targets targets, List<Protocol> relevantProtocols)
        {
            //remove redundant dependent protocols
            var remove = Dependencies.Except(relevantProtocols);
            foreach (var p in remove)
            {
                Dependencies.Remove(p);
            }

            //remove redundant dependencies
            remove = Dependants.Except(relevantProtocols);
            foreach (var p in remove)
            {
                Dependants.Remove(p);
            }

            //remove redundant switch cases
            var relevantSwitches = new List<SwitchCase>();
            foreach (var h in relevantProtocols)
            {
                relevantSwitches.AddRange(Switch.Cases.FindAll(x => x.Protocol == h.Name));
            }
            Switch.Cases.Clear();
            Switch.Cases.AddRange(relevantSwitches);

            //remove redundant parents and children
            remove = Parents.Except(relevantProtocols);
            foreach (var p in remove)
            {
                Parents.Remove(p);
            }

            remove = Children.Except(relevantProtocols);
            foreach (var p in remove)
            {
                Children.Remove(p);
            }


            //force collection of switch fieldId if not a terminal node
            if (Dependants.Count > 0) Fields.Find(x => x.ID == Switch.FieldID).RequiredInBackground = true;
            //if no dependants, node is terminal. Since only required fields are length and switch, 
            //which are irrelevant in terminal node, this can be deactivated for all fields in protocol
            //this will not affect collecting target values - only fields flagged as required due to being switch fields.
            else Fields.ForEach(x => x.RequiredInBackground = false);

            //remove redundant fields - those not set as required by switch ops or referencced in kernel programs
            Fields.RemoveAll(r => !(r.IsResult || targets.ReleventFields.Contains(r) || r.RequiredInBackground));
            foreach (var field in Fields)
            {
                field.Filters.RemoveAll(r => !(r.IsRule || Switch.Cases.Exists(c => c.Filter.Equals(r.ID))));
            }
            
        }

        public int CompareTo(Protocol other)
        {
            if (Dependants.Contains(other) && Dependencies.Contains(other)) throw new Exception("Error: Circular Reference in " + Name);
            if (Dependants.Contains(other)) //other depends on this protocol, this protocol comes first
                return -1;
            return Dependencies.Contains(other) ? 1 : 0;
        }

        /// <summary>
        ///  returns true if this protocol is in a path between the root and any target FieldID
        /// </summary>
        /// <param name="targets">explicitly referenced target fields</param>
        /// <param name="root">the root protocol</param>
        /// <returns></returns>
        public bool Required(HashSet<Field> targets, Protocol root)
        {
            return IsRule || (Dependencies.Contains(root) || root == this) && targets.Any(target => Dependants.Contains(target.Parent) || Fields.Contains(target));
        }

        public bool IsTerminal(Targets target)
        {
            var protocols = target.RelevantProtocols;
            return Dependants.All(d => !protocols.Contains(d));
        }
    }


    public class Field : IEquatable<Field>, IComparable<Field>
    {
        public Protocol Parent { get; private set; }
        public string ID { get; private set; }
        public FieldRange Range { get; private set; }

        public List<FieldFilter> Filters { get; private set; }

        public Statement Statement { get; private set; }
        /// <summary>
        /// indicates if the field is required for a switch or statment background process
        /// </summary>
        public bool RequiredInBackground { get; set; }


        public int ResultIndex { get; private set; }
        public int StoreAsResult()
        {
            if (ResultIndex == -1)
            {
                ResultIndex = MemoryCoordinator.GetIntResultIndex();
            }
            return ResultIndex;
        }
        public bool IsResult { get { return ResultIndex >= 0; } }

        public Field(string id, FieldRange range, Protocol parent)
        {
            this.ID = id;
            this.Range = range;
            Statement = null;
            RequiredInBackground = false;
            Filters = new List<FieldFilter>();
            ResultIndex = -1;
        }

        public void AddFilter(FieldFilter filter)
        {
            Filters.Add(filter);
        }

        public void SetStatement(Statement statement)
        {
            Statement = statement;
            RequiredInBackground = true;
        }
        public bool Equals(Field other)
        {
            return ID.Equals(other.ID) && Parent.Name.Equals(other.Parent.Name);
        }

        public int CompareTo(Field other)
        {
            var tmp = Range.StartOffset.CompareTo(other.Range.StartOffset);
            if (tmp == 0)
            {
                tmp = Range.EndOffset.CompareTo(other.Range.EndOffset);
            }
            return tmp;
        }
    }


    public class FieldFilter : IEquatable<FieldFilter>
    {
        public Field Parent { get; private set; }
        public string ID { get; private set; }
        public Comparison Comparison { get; private set; }
        public int LookupIndex { get; private set; }


        public int RuleIndex { get; private set; }
        public int StoreAsRule()
        {
            if (RuleIndex == -1)
            {
                RuleIndex = MemoryCoordinator.GetBoolRuleIndex();
            }
            return RuleIndex;
        }
        public bool IsRule { get { return RuleIndex >= 0; } }

        public FieldFilter(string id, Comparison comp, int value, Field parent)
        {
            ID = id;
            Comparison = comp;
            LookupIndex = MemoryCoordinator.RegisterStaticInteger(value);
            this.Parent = parent;
            RuleIndex = -1;
        }

        public bool Equals(FieldFilter other)
        {
            return ID == other.ID && Parent == other.Parent;
        }
    }

    public class Switch
    {
        public Protocol Parent { get; private set; }
        public string FieldID { get; private set; }
        public List<SwitchCase> Cases { get; private set; }
        public Field Target { get; private set; }

        public Switch(string fieldID, Protocol parent)
        {
            this.FieldID = fieldID;
            this.Parent = parent;
            Cases = new List<SwitchCase>();
        }

        public void AddCase(SwitchCase filterCase)
        {
            Cases.Add(filterCase);
        }

        /// <summary>
        /// Resolves the field from string Name using library. Should only be involked after redundancy pruning.
        /// </summary>
        /// <param name="library"></param>
        public void ResolveField()
        {
            Target = ProtocolLibrary.GetField(Parent.Name, FieldID);
            if (Parent.Switch.Cases.Count > 0) Target.RequiredInBackground = true;
        }
    }

    public class SwitchCase
    {
        public string Filter { get; private set; }
        public string Protocol { get; private set; }

        public SwitchCase(string filter, string protocol)
        {
            this.Filter = filter;
            this.Protocol = protocol;
        }
    }

    public class FieldRange : IComparable<FieldRange>
    {
        public int Length { get; private set; }
        public int StartOffset { get; private set; }
        public int EndOffset { get { return StartOffset + Length; } }

        /// <summary>
        /// the number of separate integers covered by the range
        /// </summary>
        public int ReadWidth
        {
            get
            {
                int start = StartOffset / 32;
                int end = EndOffset / 32;
                return end - start + 1;
            }
        }

        public FieldRange(int start, int length)
        {
            this.StartOffset = start;
            this.Length = length;
        }
        public int CompareTo(FieldRange other)
        {
            return StartOffset.CompareTo(other.StartOffset) != 0 ? StartOffset.CompareTo(other.StartOffset) : Length.CompareTo(other.Length);
        }

    }

    public class Statement
    {
        public Expression Expression { get; private set; }
        public Protocol Protocol { get; private set; }

        public Statement(Protocol protocol, Expression expression)
        {
            Protocol = protocol;
            Expression = expression;
        }
    }

    public class FieldComparison
    {
        public Comparison Comparison { get; private set; }
        public int Target { get; private set; }
        public int BoolRuleIndex { get; private set; }

        public FieldComparison(Comparison comparison, int target, int index)
        {
            this.Comparison = comparison;
            Target = target;
            BoolRuleIndex = index;
        }
    }

}
