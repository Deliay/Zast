using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Zast.Player.CUI
{
    public static class RoomHistory
    {
        private const string JsonFile = "history.json";

        public static async Task Add(int roomId, CancellationToken cancellationToken = default)
        {
            var exist = await GetHistory(cancellationToken);

            exist.Add(roomId);

            var tmpFileName = await Save(exist, cancellationToken);
            var tmpMoveFileName = $"{JsonFile}_tmp_mov";

            File.Move(JsonFile, tmpMoveFileName);
            File.Move(tmpFileName, JsonFile);
            File.Delete(tmpMoveFileName);
        }

        private static async Task<string> Save(HashSet<int> roomIds, CancellationToken cancellationToken)
        {
            var tmpFileName = $"{JsonFile}_tmp";
            using var tmpStream = File.OpenWrite(tmpFileName);
            await JsonSerializer.SerializeAsync(tmpStream, roomIds, cancellationToken: cancellationToken);

            return tmpFileName;
        }

        public static async Task<HashSet<int>> GetHistory(CancellationToken cancellationToken = default)
        {
            using var stream = File.Open(JsonFile, FileMode.OpenOrCreate);
            try
            {
                return await JsonSerializer.DeserializeAsync<HashSet<int>?>(stream, cancellationToken: cancellationToken)
                    ?? new HashSet<int>();
            }
            catch
            {
                return new HashSet<int>();
            }
        }
    }
}
