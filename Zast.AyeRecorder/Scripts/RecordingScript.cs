using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zast.AyeRecorder.Config;
using Zast.AyeRecorder.Recording;
using Zast.BuildingBlocks.Scripts;

namespace Zast.AyeRecorder.Scripts
{
    public class RecordingScript : IScript
    {
        public string Name => "录制";

        private readonly Dictionary<long, RecordingMan> recordingInstance = new();
        private readonly IServiceProvider serviceProvider;
        private readonly RecordConfigRepository recordConfigRepository;

        public RecordingScript(IServiceProvider serviceProvider, RecordConfigRepository recordConfigRepository)
        {
            this.serviceProvider = serviceProvider;
            this.recordConfigRepository = recordConfigRepository;
        }

        private async Task Activitor(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var config = await recordConfigRepository.Load(cancellationToken);

                if (config == null)
                {
                    return;
                }

                var roomIds = new HashSet<long>(config.RoomIds ?? new());

                foreach (var item in roomIds)
                {
                    if (!recordingInstance.ContainsKey(item))
                    {
                        recordingInstance.Add(item, serviceProvider.GetRequiredService<RecordingMan>());
                    }


                }
            }
        }

        public async ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
        {
            if (!await RecordingLock.Lock(cancellationToken))
            {
                AnsiConsole.MarkupLine("当前工作路径下，录制进程已经启动，不能重复进行录制。");
                return default!;
            }

            try
            {
                
            }
            finally
            {
                await RecordingLock.Unlock(cancellationToken);
            }
        }
    }
}
