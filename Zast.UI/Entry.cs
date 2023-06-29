using Autofac;
using EmberCore.KernelServices.UI.View;
using EmberKernel.Plugins;
using EmberKernel.Plugins.Attributes;
using EmberKernel.Plugins.Components;
using HandyControl.Tools;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using Zast.UI.Crawler;

namespace Zast.UI
{
    [EmberPlugin(Author = "ZeroAsh", Name = "Zast UI", Version = "0.0.1")]
    public class Entry : Plugin, ICoreWpfPlugin
    {
        public void BuildApplication(Application application)
        {
            application.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/HandyControl;component/Themes/Basic/Colors/Dark.xaml", UriKind.Absolute)
            });
            application.Resources.MergedDictionaries.Add(ResourceHelper.GetTheme());
        }

        public override void BuildComponents(IComponentBuilder builder)
        {
            builder.ConfigureWpfWindow<MainWindow>();
            builder.ConfigureComponent<LiveEventCrawler>().SingleInstance();
        }

        public override async ValueTask Initialize(ILifetimeScope scope)
        {
            await scope.InitializeWpfWindow<MainWindow>();

            var crawler = scope.Resolve<LiveEventCrawler>();
            await crawler.ConnectAsync();
        }

        public override async ValueTask Uninitialize(ILifetimeScope scope)
        {
            await scope.UninitializeWpfWindow<MainWindow>();
        }
    }
}