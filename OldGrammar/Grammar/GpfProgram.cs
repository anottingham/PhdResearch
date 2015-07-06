using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grammar
{
    public class GpfProgram
    {
        public List<char> RuleProgram;
        public List<char> FilterProgram;

        public ProgramMemory ProgramMemory;

        public GpfProgram(List<char> ruleProgram, List<char> filterProgram, ProgramMemory programMemory)
        {
            RuleProgram = ruleProgram;
            FilterProgram = filterProgram;
            ProgramMemory = programMemory;
        }
    }
}
