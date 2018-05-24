using MiniGMap.Core;
using System.Windows;
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
            //GMapCtrl.CacheLocation = @"D:\LOG\ProgramFiles\MapDownloader\MapCache";

            GMapCtrl.MapProvider = GMapProviders.GoogleChinaSatelliteMap;

            GMapCtrl.Manager.Mode = AccessMode.CacheOnly;
            GMapCtrl.Position = new PointLatLng(30.6898, 103.9468);
            GMapCtrl.Zoom = 14;
            GMapCtrl.MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;
            GMapCtrl.ShowCenter = false;
            GMapCtrl.DragButton = MouseButton.Right;
            GMapCtrl.ShowTileGridLines = false;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //GMapCtrl.CacheLocation = @"D:\LOG\CODE\MapDB";
            //GMapCtrl.MapProvider = GMapProviders.GoogleChinaSatelliteMap;
            //GMapCtrl.Manager.Mode = AccessMode.CacheOnly;
            //GMapCtrl.ReloadMap();
            GMapCtrl.Zoom++;
        }

        private void Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            //GMapCtrl.CacheLocation = @"D:\LOG\ProgramFiles\MapDownloader\MapCache";
            //GMapCtrl.MapProvider = GMapProviders.AMapSateliteMap;
            //GMapCtrl.Manager.Mode = AccessMode.CacheOnly;
            //GMapCtrl.ReloadMap();
            GMapCtrl.Zoom--;
        }

        private void GMapCtrl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //if (e.LeftButton == MouseButtonState.Pressed)
            //{
                
            //}
        }

        private void GMapCtrl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Point p = e.GetPosition(GMapCtrl);
            //GMapCtrl.NewPoint = GMapCtrl.FromLocalToLatLng((int)p.X, (int)p.Y);
            //GMapCtrl.UpdateBounds();
            //GMapCtrl.InvalidateVisual(true);
        }
    }
}
