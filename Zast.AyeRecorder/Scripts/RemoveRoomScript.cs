using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zast.AyeRecorder.Config;
using Zast.BuildingBlocks.Scripts;

namespace Zast.AyeRecorder.Scripts
{
    internal class RemoveRoomScript : IScript
    {
        private readonly RecordConfigRepository configRepository;

        public RemoveRoomScript(RecordConfigRepository configRepository)
        {
            this.configRepository = configRepository;
        }

        public string Name => "移除房间";

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


            config.RoomIds.Remove(roomId);

            await configRepository.Save(config, cancellationToken);

            AnsiConsole.MarkupLine($"[yellow]{roomId}[/] 移除成功");
            return default!;
        }
    }
}
