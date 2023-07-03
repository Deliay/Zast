using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zast.Player.CUI.Bilibili.Streaming.PipeUtil;

namespace Zast.Player.CUI.Bilibili.Streaming.AudioPlayer
{
    public class StreamingAudioPlayer : IDisposable
    {
        private readonly Pipe pipe;
        private readonly PipeChain chain;

        public StreamingAudioPlayer()
        {
            pipe = new Pipe();
            chain = new PipeChain()
                .Select(FFmpegAudioSelector.Convert)
                .Final(BassAudioPlayer.Play);
        }

        public Task<PipeWriter> Handle(CancellationToken cancellationToken)
        {
            _ = chain.Pipe(pipe.Reader, cancellationToken);
            return Task.FromResult(pipe.Writer);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            using var _chain = chain;
        }
    }
}
