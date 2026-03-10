using Prism.Ioc;
using Prism.Modularity;
using System.Windows;
using AutoWizard.UI.Views;

namespace AutoWizard.UI
{
    /// <summary>
    /// Prism Application Bootstrap
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 註冊服務
            // containerRegistry.RegisterSingleton<IScriptExecutor, ScriptExecutor>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            // 註冊模組
            // moduleCatalog.AddModule<CoreModule>();
        }
    }
}
