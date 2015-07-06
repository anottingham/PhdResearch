using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    class Protocol
    {
        private List<Protocol> Parents;

        private string id;

    }

    class Field
    {
        private Protocol container;
        private string id;
        private bool necessary; //necessary if includes statements (field filters are not necessary when not used)
    }
}
