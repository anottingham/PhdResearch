using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Grammar
{
    public static class GpfCompiler
    {
        public static GpfProgram CompileProgram(string programFilename)
        {
            try //lexer and parser are created during compilation - works as long as grammar compiles
            {
                GpfLexer lexer = new GpfLexer(new AntlrFileStream(programFilename));
                CommonTokenStream tokens = new CommonTokenStream(lexer);

                GpfParser parser = new GpfParser(tokens);
                parser.program(); 
                
            }
            catch (InputMismatchException exception)
            {
                Console.WriteLine(exception.ToString());
                throw;
            }
            return GpfProgramCompiler.ProgramSet.GetProgram();
        }
    }
}
