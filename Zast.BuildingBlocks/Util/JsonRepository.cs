using System.Text.Json;

namespace Zast.BuildingBlocks.Util
{
    public static class JsonRepository
    {
        public static async Task Save<T>(string path, T data, CancellationToken cancellationToken = default)
        {
            var tmpFileName = $"{path}_tmp";
            {
                if (File.Exists(tmpFileName))
                {
                    File.Delete(tmpFileName);
                }
                using var tmpStream = File.OpenWrite(tmpFileName);
                await JsonSerializer.SerializeAsync(tmpStream, data, cancellationToken: cancellationToken);
            }
            var tmpMoveFileName = $"{path}_tmp_mov";
            try
            {

                File.Move(path, tmpMoveFileName);
                File.Move(tmpFileName, path);
                File.Delete(tmpMoveFileName);
            }
            finally
            {
                if (File.Exists(tmpFileName)) File.Delete(tmpFileName);
                if (File.Exists(tmpMoveFileName)) File.Delete(tmpMoveFileName);
            }
        }

        public static async Task<T?> Load<T>(string path, CancellationToken cancellationToken = default)
        {
            using var stream = File.Open(path, FileMode.OpenOrCreate);
            try
            {
                return await JsonSerializer.DeserializeAsync<T?>(stream, cancellationToken: cancellationToken)
                    ?? default;
            }
            catch
            {
                return default;
            }
        }
    }

    public abstract class JsonRepository<T>
    {
        private static readonly SemaphoreSlim _lock = new(1);
        private readonly string path;

        public JsonRepository(string path)
        {
            this.path = path;
        }

        public virtual async Task Save(T data, CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                await JsonRepository.Save<T>(path, data, cancellationToken);
            }
            finally
            {
                _lock.Release();
            }
        }

        public virtual async Task<T?> Load(CancellationToken cancellationToken = default)
        {
            return await JsonRepository.Load<T>(path, cancellationToken);
        }
    }
}