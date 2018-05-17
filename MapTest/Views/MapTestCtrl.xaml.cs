using MiniGMap.Core;
using System.Windows.Controls;
using System.Windows.Input;

namespace MapTest.Views
{
    /// <summary>
    /// MapTestCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class MapTestCtrl : UserControl
    {
        public MapTestCtrl()
        {
            InitializeComponent();
            GMapCtrl.MapProvider = GMapProviders.GoogleChinaSatelliteMap;
            GMapCtrl.Manager.Mode = AccessMode.CacheOnly;
            GMapCtrl.Position = new PointLatLng(30.6898, 103.9468);
            GMapCtrl.Zoom = 10;
            GMapCtrl.MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;
            GMapCtrl.ShowCenter = false;
            GMapCtrl.DragButton = MouseButton.Right;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            GMapCtrl.CacheLocation = @"D:\LOG\CODE\MapDB";
            GMapCtrl.MapProvider = GMapProviders.GoogleChinaSatelliteMap;
            GMapCtrl.Manager.Mode = AccessMode.CacheOnly;
            GMapCtrl.ReloadMap();
        }

        private void Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            GMapCtrl.CacheLocation = @"D:\LOG\ProgramFiles\MapDownloader\MapCache";
            GMapCtrl.MapProvider = GMapProviders.AMapSateliteMap;
            GMapCtrl.Manager.Mode = AccessMode.CacheOnly;
            GMapCtrl.ReloadMap();
        }
    }
}
