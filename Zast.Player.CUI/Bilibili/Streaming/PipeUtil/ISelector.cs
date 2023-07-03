using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zast.Player.CUI.Bilibili.Streaming.PipeUtil
{
    public interface ISelector
    {
        Task Select(Stream @in, Stream @out, CancellationToken cancellationToken);
    }
}
