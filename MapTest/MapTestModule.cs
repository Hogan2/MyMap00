using MapTest.Views;
using Prism.Modularity;
using Prism.Regions;

namespace MapTest
{
    public class MapTestModule : IModule
    {
        IRegionManager _regionManager;
        public MapTestModule(RegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void Initialize()
        {
            _regionManager.RegisterViewWithRegion("MapRegion", typeof(MapTestCtrl));
        }
    }
}
