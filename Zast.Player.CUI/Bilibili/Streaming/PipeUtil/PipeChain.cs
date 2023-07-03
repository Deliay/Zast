using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Zast.Player.CUI.Bilibili.Streaming.PipeUtil
{
    public class PipeChain : IDisposable
    {

        public static PipeReader Pipe(Stream original, CancellationToken cancellationToken)
        {
            return Select(original, (@in, @out, c) => @in.CopyToAsync(@out, c), cancellationToken);
        }

        public static PipeReader Select(Stream original, Func<Stream, Stream, CancellationToken, Task> selector, CancellationToken cancellationToken)
        {
            var pipe = new Pipe();

            _ = selector(original, pipe.Writer.AsStream(), cancellationToken);

            return pipe.Reader;
        }

        private List<Func<Stream, Stream, CancellationToken, Task>> selectors = new();
        public PipeChain Select(Func<Stream, Stream, CancellationToken, Task> selector)
        {
            selectors.Add(selector);
            return this;
        }

        public PipeChain Select(ISelector selector)
        {
            selectors.Add(selector.Select);
            return this;
        }

        private Func<Stream, CancellationToken, Task> consumer = (_, _) => Task.CompletedTask;

        private PipeReader PipeCore(PipeReader source, CancellationToken cancellationToken)
        {
            var reader = source;
            foreach (var selector in selectors)
            {
                var stream = reader.AsStream();
                reader = Select(stream, (a, b, t) => selector(a, b, cancellationToken), cancellationToken);
            }

            return reader;
        }

        public PipeChain Final(Func<Stream, CancellationToken, Task> consumer)
        {
            this.consumer = consumer;

            return this;
        }

        public Task Pipe(PipeReader source, CancellationToken cancellationToken)
        {
            return consumer(PipeCore(source, cancellationToken).AsStream(), cancellationToken);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            selectors.Clear();
        }
    }
}
