using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Zast.BuildingBlocks.Util;
using Zast.Player.CUI.Util;

namespace Zast.Player.CUI.Bilibili
{
    public static class RoomHistory
    {
        private const string JsonFile = "history.json";

        public static async Task Add(int roomId, CancellationToken cancellationToken = default)
        {
            var exist = await GetHistory(cancellationToken);

            exist.Add(roomId);

            await JsonRepository.Save(JsonFile, exist, cancellationToken);
        }

        public static async Task<HashSet<int>> GetHistory(CancellationToken cancellationToken = default)
        {
            return (await JsonRepository.Load<HashSet<int>>(JsonFile, cancellationToken)) ?? new HashSet<int>();
        }
    }
}
