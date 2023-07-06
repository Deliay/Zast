using Mikibot.Crawler.Http.Bilibili;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zast.BuildingBlocks.Scripts;
using Zast.Player.CUI.Bilibili;

namespace Zast.Player.CUI.Scripts.Scenes
{
    public class InitializeScene : IScript
    {
        private readonly BiliLiveCrawler crawler;
        private readonly BiliBasicInfoCrawler basicInfoCrawler;
        private readonly CookieStore cookieStore;
        private readonly ZastCuiSettingRepository settingRepository;

        public InitializeScene(
            BiliLiveCrawler crawler,
            BiliBasicInfoCrawler basicInfoCrawler,
            CookieStore cookieStore,
            ZastCuiSettingRepository settingRepository)
        {
            this.crawler = crawler;
            this.basicInfoCrawler = basicInfoCrawler;
            this.cookieStore = cookieStore;
            this.settingRepository = settingRepository;
        }

        public string Name => "加载中...";

        public async ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
        {
            var config = await settingRepository.Load(cancellationToken);
            context.Set(config);

            var cookie = await cookieStore.Load(cancellationToken);

            if (!string.IsNullOrWhiteSpace(cookie.Cookie))
            {
                crawler.SetCookie(cookie.Cookie);
                basicInfoCrawler.SetCookie(cookie.Cookie);
                context.Set(cookie);
            }

            return prev;
        }
    }
}
