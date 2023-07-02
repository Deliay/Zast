using Mikibot.Crawler.WebsocketCrawler.Data.Commands;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.Utils;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zast.Player.CUI
{
    public static class CommandWriterExtension
    {
        private static CommandSubscriber cmdHandler = new CommandSubscriber();

        private static string CoinType(string name, int price, string type) => type == "gold" ? $"[yellow]{name}[/] [purple]{price / 100} 电池[/]" : $"{name}";

        private static void Handle(SendGift gift)
        {
            if (gift.CoinType == "gold" && gift.DiscountPrice > 30000)
            {
                var panel = new Panel($"[grey]礼物[/] [yellow]{gift.DiscountPrice / 100}电池[/] [teal]{gift.SenderName}[/] {gift.Action} {gift.GiftName}")
                {
                    Expand = true
                };
                AnsiConsole.Write(panel);
            }
            else
            {
                AnsiConsole.MarkupLine($"[grey]礼物[/] [teal]{gift.SenderName}[/] [grey]{gift.Action}[/] {CoinType(gift.GiftName, gift.DiscountPrice, gift.CoinType)}");
            }
        }

        private static string GuardType(string name)
        {
            return name switch
            {
                "总督" => $"[yellow]{name}[/]",
                "提督" => $"[fuchsia]{name}[/]",
                _ => $"[blue]{name}[/]",
            };
        }

        private static void Handle(GuardBuy guard)
        {
            var panel = new Panel($"[teal]{guard.UserName}[/] 开通了 {GuardType(guard.GiftName)}")
            {
                Expand = true,
            };
            AnsiConsole.Write(panel);
        }

        private static void Handle(DanmuMsg msg)
        {
            var medal = (msg.FansLevel > 0
                ? $"[grey][[[/][blue]{msg.FansTag}[/][grey]|[/][silver]{msg.FansLevel}[/][grey]]][/]"
                : "").PadRight(10, ' ');
            AnsiConsole.MarkupLine($"[grey]弹幕[/] {medal}[teal]{msg.UserName.EscapeMarkup()}[/]: [white]{msg.Msg.EscapeMarkup()}[/]");
        }

        static CommandWriterExtension()
        {
            cmdHandler.Subscribe<DanmuMsg>(Handle);
            cmdHandler.Subscribe<InteractWord>((msg) => AnsiConsole.MarkupLine($"[grey]进入[/] [grey]{msg.UserName.EscapeMarkup()}[/] [grey]进入直播间[/]"));
            cmdHandler.Subscribe<OnlineRankCount>((ranking) => AnsiConsole.MarkupLine($"当前在线人数 [green]{ranking.Count}[/]"));
            cmdHandler.Subscribe<SendGift>(Handle);
            cmdHandler.Subscribe<SuperChatMessage>((sc) => AnsiConsole.Write(new Panel($"[grey]SC[/] [yellow]{sc.Price * 10}电池[/] [teal]{sc.User.UserName}[/]: [white]{sc.Message}[/]")
            {
                Expand = true,
            }));
            cmdHandler.Subscribe<GuardBuy>(Handle);
        }

        public static async ValueTask Write(this ICommandBase @base)
        {
            await cmdHandler.Handle(@base);
        }
    }
}
