using System.Collections.Generic;
using System.Linq;

namespace Grammar
{
    /// <summary>
    /// Generates and contains the set of referenced protocol attributes in the kernel set. These are used to mark which areas of each protocol are relevant.
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
            Protocols = new HashSet<Protocol>();    //all directly referenced protocols in kernels
            Filters = new HashSet<FieldFilter>();   //all directly referenced field filters in kernels
            Fields = new HashSet<Field>();          //all directly referenced fields in kernels

            foreach (var k in kernels)
            {
                GenerateTargets(k);
            }

            var protocols = new List<Protocol>(Protocols);  //add the 
            protocols.AddRange(Fields.Select(f => f.Parent).ToList());
            protocols.AddRange(Filters.Select((f => f.Parent.Parent)).ToList());

            //referenced protocols do not affect field requirements
            var fields = new List<Field>(Fields);
            fields.AddRange(Filters.Select(f => f.Parent));


            ReleventFields = new HashSet<Field>(fields);
            RelevantProtocols = new HashSet<Protocol>(protocols);
            
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