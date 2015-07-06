using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Grammar
{
    /// <summary>
    /// Generates and contains the set of referenced protocol attributes in the kernel set. These are used to mark which areas of each protocol are relevant.
    /// Also resolves switch statments in protocols, as these are needed to update prior protocols of new filters if a protocol is referenced direectly.
    /// i.e. "filter f = IP;" needs to be "converted to filter f = Ethernet.EtherType.IP;" so IP protocol can be omitted.
    /// </summary>
    public class Targets
    {
        //protocol targets - since these targets do not ref any protocol specific logic, each translates to the logical or of relevant parent switch filters
        public HashSet<Protocol> Protocols { get; private set; }
        public HashSet<FieldFilter> Filters { get; private set; }
        public HashSet<Field> Fields { get; private set; }

        public HashSet<Field> ReleventFields { get; private set; } 
        public HashSet<Protocol> RelevantProtocols { get; private set; }

        public Targets(IEnumerable<Kernel> kernels)
        {
            Protocols = new HashSet<Protocol>(); //all directly referenced protocols in kernels
            Filters = new HashSet<FieldFilter>(); //all directly referenced field filters in kernels
            Fields = new HashSet<Field>(); //all directly referenced fields in kernels
            ReleventFields = new HashSet<Field>();
            RelevantProtocols = new HashSet<Protocol>();

            //resolve switch statements to actual fields and protocols
            ProtocolLibrary.Protocols.Values.Where(p => p.Switch != null)
                .ToList()
                .ForEach(s => s.Switch.ResolveField());

            foreach (var k in kernels)
            {
                GenerateTargets(k);
            }


            var protocols = new List<Protocol>();   //directly referenced protocols in Protocols are not required
            protocols.AddRange(Fields.Select(f => f.Parent));
            protocols.AddRange(Filters.Select((f => f.Parent.Parent)));
            protocols.AddRange(Protocols.SelectMany(p => p.RuleFields.Select(r => r.Parent)));

            //referenced protocols do not affect field requirements
            var fields = new List<Field>(Fields);
            fields.AddRange(Filters.Select(f => f.Parent));
            fields.AddRange(Protocols.SelectMany(f => f.RuleFields));

            ReleventFields.UnionWith(fields);
            RelevantProtocols.UnionWith(protocols);
        }

        public void GenerateTargets(Kernel kernel)
        {
            switch (kernel.Type)
            {
                case KernelType.Field:
                    var field = ProtocolLibrary.GetField(((FieldKernel) kernel).Protocol,
                        ((FieldKernel) kernel).Field);
                    field.StoreAsResult();
                    Fields.Add(field);
                    break;
                case KernelType.Filter:
                    var k = (FilterKernel) kernel;
                    var protocols = ProtocolLibrary.GetProtocols(k.Filter.ProtocolTargets);
                    protocols.ForEach(p => p.StoreAsRule());
                    Protocols.UnionWith(protocols);

                    var filters = ProtocolLibrary.GetFieldFilters(k.Filter.FilterTargets);
                    filters.ForEach(ff => ff.StoreAsRule());
                    Filters.UnionWith(filters);
                        
                    break;
            }
        }
    }
}