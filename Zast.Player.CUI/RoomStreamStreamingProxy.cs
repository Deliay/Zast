using Mikibot.Crawler.Http.Bilibili;
using SimpleHttpServer.Host;
using SimpleHttpServer.Pipeline;
using SimpleHttpServer.Pipeline.Middlewares;
using SimpleHttpServer.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zast.Player.CUI
{
    public class RoomStreamStreamingProxy
    {
        private readonly SimpleHost host;
        private readonly BiliLiveCrawler crawler;
        private readonly int roomId;
        private static readonly HttpClient HttpClient = new();
        private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.80 Safari/537.36 Edg/98.0.1108.50";
        static RoomStreamStreamingProxy()
        {
            HttpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            HttpClient.DefaultRequestHeaders.Add("Referer", "https://live.bilibili.com/");
            HttpClient.DefaultRequestHeaders.Add("Origin", "https://live.bilibili.com");
        }
        public RoomStreamStreamingProxy(BiliLiveCrawler crawler, int roomId)
        {
            this.host = new SimpleHostBuilder()
                .ConfigureServer(server => server.ListenLocalPort(11111))
                .Build();
            this.crawler = crawler;
            this.roomId = roomId;
        }

        private Random _random = new();
        private async ValueTask<string?> GetLiveStreamAddress(CancellationToken token)
        {
            var realRoomid = await crawler.GetRealRoomId(roomId, token);
            var allAddresses = await crawler.GetLiveStreamAddress(realRoomid, token);
            if (allAddresses.Count <= 0) return default;

            return allAddresses[_random.Next(0, allAddresses.Count - 1)].Url;
        }

        public async ValueTask Route(RequestContext ctx, Func<ValueTask> next)
        {
            using var csc = CancellationTokenSource.CreateLinkedTokenSource(ctx.CancelToken);
            csc.CancelAfter(TimeSpan.FromSeconds(30));

            var url = await GetLiveStreamAddress(csc.Token);

            using var res = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, csc.Token);
            using var stream = await res.Content.ReadAsStreamAsync(csc.Token);

            foreach (var item in res.Headers)
            {
                var key = item.Value.FirstOrDefault();
                if (key is not null)
                    ctx.Http.Response.AddHeader(item.Key, key);
            }

            ctx.Http.Response.StatusCode = 200;
            ctx.Http.Response.ContentType = res.Content.Headers.ContentType?.ToString();

            try
            {
                await stream.CopyToAsync(ctx.Http.Response.OutputStream, 8192, csc.Token);
            }
            finally
            {
                ctx.Http.Response.Close();
            }
            
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {

            host.AddHandlers(h => h.Use(RouterMiddleware.Route("/streaming", r => r.Use(Route))));

            await host.Run(cancellationToken);
        }
    }
}
