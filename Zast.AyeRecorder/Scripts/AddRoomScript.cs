using Mikibot.Crawler.Http.Bilibili;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zast.AyeRecorder.Config;
using Zast.BuildingBlocks.Scripts;

namespace Zast.AyeRecorder.Scripts;

public class AddRoomScript(RecordConfigRepository configRepository, BiliLiveCrawler crawler)
    : IScript
{
    public string Name => "添加录制";

    public async ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
    {
        var config = await configRepository.Load(cancellationToken) ?? RecordConfig.Default();

        var args = Environment.GetCommandLineArgs();

        if (args.Length < 3)
        {
            AnsiConsole.MarkupLine("[red]必须输入房间号[/]");
            return default!;
        }

        var rawRoomId = args[2];
        if (!int.TryParse(rawRoomId, out var roomId))
        {
            AnsiConsole.MarkupLine($"{rawRoomId} 不是合法的房间号");
        }

        _ = await crawler.GetLiveRoomInfo(roomId, cancellationToken);

        config.RoomIds.Add(roomId);

        await configRepository.Save(config, cancellationToken);
        AnsiConsole.MarkupLine($"[yellow]{roomId}[/] 添加成功");
        return default!;
    }
}