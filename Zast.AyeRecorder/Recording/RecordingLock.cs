using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Zast.AyeRecorder.Recording
{
    public static class RecordingLock
    {
        private const string LockFile = ".zar.lock";


        public static async ValueTask<bool> IsRunning(CancellationToken cancellationToken)
        {
            if (!File.Exists(LockFile))
            {
                return false;
            }

            if (!int.TryParse(await File.ReadAllTextAsync(LockFile, cancellationToken), out var pid))
            {
                return false;
            }
            try
            {
                return Process.GetProcessById(pid) is not null;
            }
            catch
            {
                return false;
            }
        }

        public static async ValueTask<bool> Lock(CancellationToken cancellationToken)
        {
            if (await IsRunning(cancellationToken))
            {
                return false;
            }
            await File.WriteAllTextAsync(LockFile, $"{Environment.ProcessId}", cancellationToken);
            return true;
        }

        public static async ValueTask Unlock(CancellationToken cancellationToken)
        {
            if (!await IsRunning(cancellationToken))
            {
                return;
            }

            if (!int.TryParse(await File.ReadAllTextAsync(LockFile, cancellationToken), out var pid))
            {
                return;
            }

            if (Environment.ProcessId != pid)
            {
                throw new InvalidOperationException("Unlocking failed, lock owned by other process");
            }

            File.Delete(LockFile);
        }
    }
}
