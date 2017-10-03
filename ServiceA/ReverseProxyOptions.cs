using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceA
{
    public class ReverseProxyOptions
    {
        public string Host { get; set; }

        public int Port { get; set; }

        public string Scheme { get; set; }
    }
}
