using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EmberCore.KernelServices.Plugins
{
    public interface IResolverContext
    {
        string CurrentPath { get; }
        IEnumerable<Assembly> LoadAssemblies();
    }
}
