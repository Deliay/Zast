using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zast.Player.CUI
{
    public class BoostrapConfiguration : CommandSettings
    {
        [CommandArgument(0, "[roomId]")]
        public int RoomId { get; set; }
    }
}
