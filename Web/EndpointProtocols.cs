using System;

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
