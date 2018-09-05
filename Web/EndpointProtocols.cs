using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web
{
    [Flags]
    public enum EndpointProtocols
    {
        Http = 0,
        Https = 1,
        Both = Http | Https
    }
}
