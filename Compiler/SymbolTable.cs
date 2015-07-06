using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    class SymbolTable
    {
    }

    enum SymbolType
    {
        Variable,
        Field,
        Filter,
        Protocol
    }

    class Symbol
    {
        private SymbolType type;
        private string id;
        private Dictionary<string, Symbol> scope;

    }
}
