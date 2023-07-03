using FFMpegCore.Pipes;
using FFMpegCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFMpegCore.Enums;

namespace Zast.Player.CUI.Bilibili.Streaming.AudioPlayer
{
    public static class FFmpegAudioSelector
    {
        public static async Task Convert(Stream @in, Stream @out, CancellationToken cancellationToken)
        {
            await FFMpegArguments
                .FromPipeInput(new StreamPipeSource(@in))
                .OutputToPipe(new StreamPipeSink(@out), opt => opt
                    .DisableChannel(Channel.Video)
                    .WithCustomArgument("-osr 16000 -f wav"))
                .ProcessAsynchronously();
        }
    }
}
