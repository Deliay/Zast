using EmberKernel;
using EmberKernel.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zast.Core;

namespace EmberCore.KernelServices.Plugins
{
    public class PluginResolver : IKernelService
    {
        private IOptionsSnapshot<ZastConfiguration> AppSetting { get; }
        private ILogger<PluginResolver> Logger { get; }
        private IContentRoot ContentRoot { get; }
        private readonly Dictionary<string, ResolverContext> LoadedContexts = new Dictionary<string, ResolverContext>();
        public PluginResolver(IOptionsSnapshot<ZastConfiguration> coreAppSetting, ILogger<PluginResolver> logger, IContentRoot contentRoot)
        {
            AppSetting = coreAppSetting;
            Logger = logger;
            ContentRoot = contentRoot;
        }

        private void CreateCache(string srcPath, string targetPath)
        {
            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);
            var dirsToProcess = new Queue<(string sourcePath, string destinationPath)>();
            dirsToProcess.Enqueue((sourcePath: srcPath, destinationPath: targetPath));
            while (dirsToProcess.Any())
            {
                (string sourcePath, string destinationPath) = dirsToProcess.Dequeue();

                if (!Directory.Exists(destinationPath))
                    Directory.CreateDirectory(destinationPath);

                var sourceDirectoryInfo = new DirectoryInfo(sourcePath);
                foreach (FileInfo sourceFileInfo in sourceDirectoryInfo.EnumerateFiles())
                    sourceFileInfo.CopyTo(Path.Combine(destinationPath, sourceFileInfo.Name), true);

                foreach (DirectoryInfo sourceSubDirectoryInfo in sourceDirectoryInfo.EnumerateDirectories())
                    dirsToProcess.Enqueue((
                        sourcePath: sourceSubDirectoryInfo.FullName,
                        destinationPath: Path.Combine(destinationPath, sourceSubDirectoryInfo.Name)));
            }
        }

        public IEnumerable<IResolverContext> EnumerateLoaders()
        {
            var pluginBaseFolder = Path.Combine(ContentRoot.ContentDirectory, AppSetting.Value.PluginsFolder);
            var cacheFolder = Path.Combine(ContentRoot.ContentDirectory, AppSetting.Value.PluginsCacheFolder);
            if (!Directory.Exists(pluginBaseFolder))
            {
                yield break;
            }
            var sourcePluginFolders = Directory.EnumerateDirectories(pluginBaseFolder);
            try
            {
                Directory.Delete(cacheFolder, true);
            }
            catch { }
            foreach (var folder in sourcePluginFolders)
            {
                string cache = Path.Combine(cacheFolder, $"{Path.GetFileName(folder)}_{Path.GetRandomFileName()}");
                CreateCache(folder, cache);
                LoadedContexts.Add(folder, new ResolverContext(cache, Logger));
                yield return LoadedContexts[folder];
            }
        }
    }
}
