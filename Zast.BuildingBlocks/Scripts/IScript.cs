using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zast.BuildingBlocks.Scripts
{
    public interface IScript
    {
        string Name { get; }

        ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken);
    }
}
