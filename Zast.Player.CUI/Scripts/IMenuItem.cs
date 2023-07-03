using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zast.Player.CUI.Scripts
{
    public interface IMenuItem : IScript
    {
        public Type Category { get; }
    }
}
