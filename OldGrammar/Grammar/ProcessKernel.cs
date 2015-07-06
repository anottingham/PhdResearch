using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Grammar
{

    public enum KernelType
    {
        Filter,
        Field
    }

    public class ProcessKernel
    {
        public List<Kernel> Kernels;
        public Targets Targets;

        public ProcessKernel()
        {
            Kernels = new List<Kernel>();
        }

        public void Clear()
        {
            Kernels.Clear();
            Targets = null;
        }

        public void AddKernel(Kernel kernel)
        {
            Kernels.Add(kernel);
        }

        public void BuildKernelProgram()
        {
            Targets = new Targets(Kernels); 
        }
    }

    public abstract class Kernel
    {
        public KernelType Type { get; private set; }
        public string ID { get; private set; }

        protected Kernel(KernelType type, string id)
        {
            Type = type;
            ID = id;
        }
    }

    public class FilterKernel : Kernel
    {
        public Predicate Filter { get; private set; }


        public FilterKernel(string id, Predicate filter)
            : base(KernelType.Filter, id)
        {
            this.Filter = filter;
        }
    }

    public class FieldKernel : Kernel
    {
        public string Protocol { get; private set; }
        public string Field { get; private set; }

        public FieldKernel(string id, string protocol, string field)
            : base(KernelType.Field, id)
        {
            Protocol = protocol;
            Field = field;
        }
    }



}
