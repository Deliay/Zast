using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zast.Core
{
    public class Logging
    {
        public bool IncludeScopes { get; set; }
        public Loglevel LogLevel { get; set; }
    }

    public class Loglevel
    {
        public string Default { get; set; }
        public string System { get; set; }
        public string EmberCore { get; set; }
    }

    public class ZastConfiguration
    {
        public bool CheckUpdate { get; set; }
        public bool CheckComponentUpdate { get; set; }
        public string Locale { get; set; }
        public string PluginsFolder { get; set; }
        public string PluginsCacheFolder { get; set; }
        public int CommandSourceOperationTimeLimit { get; set; }
        public Logging Logging { get; set; }
    }
}
