using ManagedBass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zast.Player.CUI.Bilibili
{
    public class UrlBassPlayer : IDisposable
    {
        public event Action<long>? LoadingProgressUpdated;

        private int streamIdx;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Bass.StreamFree(streamIdx);
            Bass.Free();
        }

        public IDisposable Play(string url, CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => Bass.Stop());
            Bass.Init();
            this.streamIdx = Bass.CreateStream(url, 0, BassFlags.Default, (_, p, _) => LoadingProgressUpdated?.Invoke(p));
            Bass.ChannelPlay(this.streamIdx);
            return this;
        }
    }
}
