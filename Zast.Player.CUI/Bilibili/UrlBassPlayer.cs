using ManagedBass;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Zast.Player.CUI.Bilibili
{
    public class UrlBassPlayer : IDisposable
    {
        public event Action<long, TimeSpan>? LoadingProgressUpdated;

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
            
            var flag = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? DeviceInitFlags.DMix
                : DeviceInitFlags.Default;

            AnsiConsole.MarkupLine($"[grey]BASS[/] Bass run in {flag}");

            if (!Bass.Init(Flags: flag))
                throw new InvalidOperationException("BASS initialize failed");

            this.streamIdx = Bass.CreateStream(url, 0, BassFlags.Default, (_, p, _) =>
            {
                var time = Bass.ChannelBytes2Seconds(this.streamIdx, Bass.ChannelGetPosition(this.streamIdx));
                LoadingProgressUpdated?.Invoke(p, TimeSpan.FromSeconds(time));
            });
            Bass.ChannelPlay(this.streamIdx);
            return this;
        }
    }
}
