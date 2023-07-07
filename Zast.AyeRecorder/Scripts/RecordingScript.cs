using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zast.AyeRecorder.Config;
using Zast.AyeRecorder.Recording;
using Zast.AyeRecorder.Script.Config;
using Zast.BuildingBlocks.Scripts;

namespace Zast.AyeRecorder.Scripts
{
    public class RecordingScript : IScript
    {
        public string Name => "录制";

        private readonly Dictionary<long, RecordingMan> recordingInstance = new();
        private readonly IServiceProvider serviceProvider;
        private readonly RecordConfigRepository recordConfigRepository;
        private readonly ILogger<RecordingScript> logger;

        public RecordingScript(
            IServiceProvider serviceProvider,
            RecordConfigRepository recordConfigRepository,
            ILogger<RecordingScript> logger)
        {
            this.serviceProvider = serviceProvider;
            this.recordConfigRepository = recordConfigRepository;
            this.logger = logger;
        }

        private async Task IntervalActivate(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Activitor(cancellationToken);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        private async Task Activitor(CancellationToken cancellationToken)
        {
            var config = await recordConfigRepository.Load(cancellationToken)
                ?? RecordConfig.Default();

            if (config == null)
            {
                return;
            }

            var roomIds = new HashSet<long>(config.RoomIds ?? new());
            if (roomIds.Count == 0)
                logger.LogInformation("暂未有需要录制的房间");

            foreach (var roomId in roomIds)
            {
                if (!recordingInstance.ContainsKey(roomId))
                {
                    logger.LogInformation($"正在启用房间监听 {roomId}");

                    var man = serviceProvider.GetRequiredService<RecordingMan>();
                    await man.Initialize(roomId);

                    logger.LogInformation($"{roomId} 监听启动成功");

                    recordingInstance.Add(roomId, man);
                }
            }

            var removedIds = new HashSet<long>();
            foreach (var roomId in recordingInstance.Keys)
            {
                if (!roomIds.Contains(roomId))
                {
                    logger.LogInformation($"准备停止录制 {roomId}");

                    using var instnace = recordingInstance[roomId];
                    removedIds.Add(roomId);

                    logger.LogInformation($"已停止录制 {roomId}");
                }
            }

            foreach (var roomId in removedIds)
            {
                recordingInstance.Remove(roomId);
            }
        }

        public async ValueTask<IScript> Show(IScript prev, ScriptContext context, CancellationToken cancellationToken)
        {
            if (!await RecordingLock.Lock(cancellationToken))
            {
                logger.LogWarning("当前工作路径下，录制进程已经启动，不能重复进行录制。");
                return default!;
            }
            AnsiConsole.Write(new FigletText("Aye Recorder"));
            logger.LogInformation("Aye Recorder 启动中");
            try
            {
                await IntervalActivate(cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "出错啦");
            }
            finally
            {
                await RecordingLock.Unlock(cancellationToken);
            }

            return default!;
        }
    }
}
