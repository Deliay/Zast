using Autofac;
using EmberKernel.Plugins.Components;
using EmberKernel.Services.UI.Mvvm.ViewComponent.Window;
using HandyControl.Controls;
using System;
using System.Threading.Tasks;
using Zast.UI.Crawler;

namespace Zast.UI
{
    /// <summary>
    /// Interaction logic for NewWindow.xaml
    /// </summary>
    public partial class MainWindow : GlowWindow, IHostedWindow, IComponent
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public ValueTask Initialize(ILifetimeScope scope)
        {
            this.List.ItemsSource = scope.Resolve<LiveEventCrawler>();
            Show();
            return default;
        }

        public ValueTask Uninitialize(ILifetimeScope scope)
        {
            return default;
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
