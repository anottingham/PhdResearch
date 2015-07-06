using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Grammar
{
    public class GpfCompiler
    {
        public GpfCompiler(string programFilename)
        {
            GpfLexer lexer = new GpfLexer(new AntlrFileStream(programFilename));
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            GpfParser parser = new GpfParser(tokens);

            parser.program();

            ProgramSet set = GpfProgramCompiler.ProgramSet;
            set.Generate();
        }
    }
}
