using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace Grammar
{
    public static class GpfProgramCompiler
    {
        public static List<ProgramLayer> Layers { get; private set; }
        public static ProgramSet ProgramSet { get; private set; } 


        static GpfProgramCompiler()
        {
            Layers = new List<ProgramLayer>();
            ProgramSet = new ProgramSet();
        }

        public static void Clear()
        {
            Layers.Clear();
            ProgramSet = null;
        }
        public static void Compile(List<Protocol> protocols)
        {
            Layers.Clear();
            ProgramSet.RuleProgram.Clear();

            foreach (var p in protocols)
            {
                AddProtocol(p);
            }

            Validate();
            
            //generate the program set beased on statically available Layers var
            ProgramSet.Generate();
            
        }

        /// <summary>
        /// Adds a protocol to the progrm tree. It is inserted to a suitable existing layer, or a new layer if no suitable layers are available.
        /// </summary>
        /// <param name="protocol">the protocol to add</param>
        private static void AddProtocol(Protocol protocol)
        {
            int index = 0;
            while (Layers[index].CompareToProtocol(protocol) < 0 && index < Layers.Count) index++;

            if (index == Layers.Count) Layers.Add(new ProgramLayer());
            else if (Layers[index].CompareToProtocol(protocol) > 0) Layers.Insert(index, new ProgramLayer());
            
            Layers[index].AddProtocol(protocol);
        }

        public static void Validate()
        {
            Layers.ForEach(layer => layer.Validate());
        }
    }

    public class ProgramLayer
    {
        public HashSet<Protocol> Protocols { get; private set; }

        public HashSet<Protocol> Prerequisits { get; private set; }
        public HashSet<Protocol> Dependants { get; private set; } 

        public ProgramLayer()
        {
            Protocols = new HashSet<Protocol>();
            Prerequisits = new HashSet<Protocol>();
            Dependants = new HashSet<Protocol>();
        }

        public void AddProtocol(Protocol protocol)
        {
            Protocols.Add(protocol);
            Prerequisits.UnionWith(protocol.Dependencies);
            Dependants.UnionWith(protocol.Dependants);
        }

        public int CompareToProtocol(Protocol other)
        {
            if (Dependants.Contains(other) && Prerequisits.Contains(other)) throw new Exception("Error: Circular Reference when adding " + other.Name);
            if (Dependants.Contains(other)) //other depends on this protocol, this protocol comes first
                return -1;
            return Prerequisits.Contains(other) ? 1 : 0;
        }

        public void Validate()
        {
            foreach (var protocol in Protocols)
            {
                if (Prerequisits.Contains(protocol))
                {
                    throw new Exception("ProgramLayer dependency error: Protocol " + protocol.Name + " is a prerequisit of the layer it is in.");
                }
                if (Dependants.Contains(protocol))
                {
                    throw new Exception("ProgramLayer dependency error: Protocol " + protocol.Name + " is a dependant of the layer it is in.");
                }
            }
        }
    }


}
