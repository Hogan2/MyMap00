using MapTest;
using Prism.Modularity;
using Prism.Unity;
using Microsoft.Practices.Unity;
using MyMainWindow.Views;
using System.Windows;

namespace MyMainWindow
{
    class Bootstrapper : UnityBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }
        protected override void InitializeShell()
        {
            Application.Current.MainWindow.Show();
        }
        protected override void ConfigureModuleCatalog()
        {
            var catalog = (ModuleCatalog)ModuleCatalog;
            catalog.AddModule(typeof(MapTestModule));
        }
    }
}
