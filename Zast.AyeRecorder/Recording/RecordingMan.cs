using Mikibot.Crawler.Http.Bilibili;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zast.AyeRecorder.Recording
{
    public class RecordingMan : IDisposable
    {
        private readonly BiliLiveCrawler crawler;
        private long roomId;

        public RecordingMan(BiliLiveCrawler crawler)
        {
            this.crawler = crawler;
        }

        public ValueTask Initialize(long roomId)
        {
            this.roomId = roomId;

            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
