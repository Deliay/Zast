using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zast.Player.CUI.Bilibili.Streaming.PipeUtil
{
    public class PipeMulticaster : IDisposable
    {
        private readonly Stream source;
        private readonly CancellationTokenSource csc;

        public PipeMulticaster(Stream source)
        {
            this.source = source;
            csc = new CancellationTokenSource();
        }

        private readonly List<Func<CancellationToken, Task<PipeWriter>>> writerFactories = new();

        private readonly SemaphoreSlim _lock = new(1);

        public void To(Func<CancellationToken, Task<PipeWriter>> writerFactory)
        {
            if (_lock.Wait(TimeSpan.FromMilliseconds(100)))
            {
                try
                {
                    writerFactories.Add(writerFactory);
                }
                finally
                {
                    _lock.Release();
                }
            }
            else 
                throw new InvalidDataException();
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            if (!await _lock.WaitAsync(TimeSpan.FromMilliseconds(100), cancellationToken))
            {
                throw new InvalidOperationException();
            }

            try
            {
                using var _runCsc = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, csc.Token);
                var token = _runCsc.Token;

                var pipe = new Pipe();
                _ = source.CopyToAsync(pipe.Writer.AsStream(), token);

                var res = await pipe.Reader.ReadAsync(token);
                while (!res.IsCompleted && !res.IsCanceled)
                {
                    foreach (var writerFactory in writerFactories)
                    {
                        var writer = await writerFactory(token);
                        await writer.WriteAsync(res.Buffer.First, token);
                    }
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            using var _csc = csc;
            using var __lock = _lock;
        }
    }
}
