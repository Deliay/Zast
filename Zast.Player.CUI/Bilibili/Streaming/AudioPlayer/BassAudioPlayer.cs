using ManagedBass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zast.Player.CUI.Bilibili.Streaming.AudioPlayer
{
    public static class BassAudioPlayer
    {
        static BassAudioPlayer()
        {
            //Bass.Init();
        }

        public static Task Play(Stream @in, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
