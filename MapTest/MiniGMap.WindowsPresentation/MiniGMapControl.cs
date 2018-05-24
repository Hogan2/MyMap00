using MiniGMap.Core;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MiniGMap.WindowsPresentation
{
    public class MiniGMapControl : ItemsControl, IMInterface, IDisposable
    {
        readonly Core.Core Core = new Core.Core();
        internal ScaleTransform MapScaleTransform = new ScaleTransform();
        readonly RotateTransform rotationMatrix = new RotateTransform();
        GeneralTransform rotationMatrixInvert = new RotateTransform();
        FormattedText Copyright;
        readonly ScaleTransform lastScaleTransform = new ScaleTransform();
        Application loadedApp;
        bool lazyEvents = true;
        RectLatLng? lazySetZoomToFitRect = null;
        static DataTemplate DataTemplateInstance;
        static ItemsPanelTemplate ItemsPanelTemplateInstance;
        static Style StyleInstance;
        public bool ShowCenter = true;
        public Brush SelectedAreaFill = new SolidColorBrush(Color.FromArgb(33, Colors.RoyalBlue.R, Colors.RoyalBlue.G, Colors.RoyalBlue.B));
        public Brush EmptytileBrush = Brushes.Navy;
        public Pen EmptyTileBorders = new Pen(Brushes.White, 1.0);
        public FormattedText EmptyTileText = new FormattedText("We are sorry, but we don't\nhave imagery at this zoom\n     level for this region.", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Arial"), 16, Brushes.Blue);
        Typeface tileTypeface = new Typeface("Arial");
        public MouseButton DragButton = MouseButton.Right;
        public readonly ObservableCollection<GMapMarker> Markers = new ObservableCollection<GMapMarker>();


        public MiniGMapControl()
        {
            if (!DesignModeInConstruct)
            {
                #region -- templates --



                if (DataTemplateInstance == null)
                {
                    DataTemplateInstance = new DataTemplate(typeof(GMapMarker));
                    {
                        FrameworkElementFactory fef = new FrameworkElementFactory(typeof(ContentPresenter));
                        fef.SetBinding(ContentPresenter.ContentProperty, new Binding("Shape"));
                        DataTemplateInstance.VisualTree = fef;
                    }
                }
                ItemTemplate = DataTemplateInstance;

                if (ItemsPanelTemplateInstance == null)
                {
                    var factoryPanel = new FrameworkElementFactory(typeof(Canvas));
                    {
                        factoryPanel.SetValue(Canvas.IsItemsHostProperty, true);

                        ItemsPanelTemplateInstance = new ItemsPanelTemplate();
                        {
                            ItemsPanelTemplateInstance.VisualTree = factoryPanel;
                        }
                    }
                }
                ItemsPanel = ItemsPanelTemplateInstance;

                if (StyleInstance == null)
                {
                    StyleInstance = new Style();
                    {
                        StyleInstance.Setters.Add(new Setter(Canvas.LeftProperty, new Binding("LocalPositionX")));
                        StyleInstance.Setters.Add(new Setter(Canvas.TopProperty, new Binding("LocalPositionY")));
                        StyleInstance.Setters.Add(new Setter(Canvas.ZIndexProperty, new Binding("ZIndex")));
                    }
                }
                ItemContainerStyle = StyleInstance;
                #endregion

                ClipToBounds = true;
                SnapsToDevicePixels = true;

                Core.SystemType = "WindowsPresentation";

                Core.RenderMode = RenderMode.WPF;

                Core.OnMapZoomChanged += new MapZoomChanged(ForceUpdateOverlays);
                Loaded += new RoutedEventHandler(GMapControl_Loaded);
                Dispatcher.ShutdownStarted += new EventHandler(Dispatcher_ShutdownStarted);
                SizeChanged += new SizeChangedEventHandler(GMapControl_SizeChanged);

                // by default its internal property, feel free to use your own
                if (ItemsSource == null)
                {
                    ItemsSource = Markers;
                }

                Core.Zoom = (int)((double)ZoomProperty.DefaultMetadata.DefaultValue);
            }
        }

        static MiniGMapControl()
        {
            GMapImageProxy.Enable();

            GMaps.Instance.SQLitePing();

        }

        public void ReloadMap()
        {
            Core.ReloadMap();
        }
        public void UpdateBounds()
        {
            //Core.CancelAsyncTasks();
            Core.Matrix.ClearLevelAndPointsIn(Core.Zoom, Core.corepos);
            //Core.Refresh.Set();
            Core.UpdateBounds();
        }
        protected bool DesignModeInConstruct
        {
            get
            {
                return System.ComponentModel.DesignerProperties.GetIsInDesignMode(this);
            }
        }

        public GMaps Manager
        {
            get
            {
                return GMaps.Instance;
            }
        }
        public bool CanDragMap
        {
            get
            {
                return Core.CanDragMap;
            }
            set
            {
                Core.CanDragMap = value;
            }
        }
        bool isSelected = false;
        PointLatLng selectionEnd;
        PointLatLng selectionStart;
        
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (CanDragMap && e.ChangedButton == DragButton)
            {
                Point p = e.GetPosition(this);

                if (MapScaleTransform != null)
                {
                    p = MapScaleTransform.Inverse.Transform(p);
                }

                p = ApplyRotationInversion(p.X, p.Y);

                Core.mouseDown.X = (int)p.X;
                Core.mouseDown.Y = (int)p.Y;

                InvalidateVisual();
            }
            else
            {
                if (!isSelected)
                {
                    Point p = e.GetPosition(this);
                    isSelected = true;
                    SelectedArea = RectLatLng.Empty;
                    selectionEnd = PointLatLng.Empty;
                    selectionStart = FromLocalToLatLng((int)p.X, (int)p.Y);
                }
            }
        }
        bool isDragging = false;
        int onMouseUpTimestamp = 0;
        public RectLatLng? BoundsOfMap = null;
        public event SelectionChange OnSelectionChange;
        public delegate void SelectionChange(RectLatLng Selection, bool ZoomToFit);

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            
            if (isSelected)
            {
                isSelected = false;
            }

            if (Core.IsDragging)
            {
                if (isDragging)
                {
                    onMouseUpTimestamp = e.Timestamp & Int32.MaxValue;
                    isDragging = false;
                    Debug.WriteLine("IsDragging = " + isDragging);
                    Cursor = cursorBefore;
                    Mouse.Capture(null);
                }
                Core.EndDrag();

                if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
                {
                    if (Core.LastLocationInBounds.HasValue)
                    {
                        Position = Core.LastLocationInBounds.Value;
                    }
                }
            }
            else
            {
                if (e.ChangedButton == DragButton)
                {
                    Core.mouseDown = GPoint.Empty;
                }

                if (!selectionEnd.IsEmpty && !selectionStart.IsEmpty)
                {
                    bool zoomtofit = false;

                    if (!SelectedArea.IsEmpty && Keyboard.Modifiers == ModifierKeys.Shift)
                    {
                        zoomtofit = SetZoomToFitRect(SelectedArea);
                    }

                    OnSelectionChange?.Invoke(SelectedArea, zoomtofit);
                }
                else
                {
                    InvalidateVisual();
                }
            }
        }

        Cursor cursorBefore = Cursors.Arrow;
        public bool DisableAltForSelection = false;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            // wpf generates to many events if mouse is over some visual
            // and OnMouseUp is fired, wtf, anyway...
            // http://greatmaps.codeplex.com/workitem/16013
            if ((e.Timestamp & Int32.MaxValue) - onMouseUpTimestamp < 55)
            {
                Debug.WriteLine("OnMouseMove skipped: " + ((e.Timestamp & Int32.MaxValue) - onMouseUpTimestamp) + "ms");
                return;
            }

            if (!Core.IsDragging && !Core.mouseDown.IsEmpty)
            {
                Point p = e.GetPosition(this);

                if (MapScaleTransform != null)
                {
                    p = MapScaleTransform.Inverse.Transform(p);
                }

                p = ApplyRotationInversion(p.X, p.Y);

                // cursor has moved beyond drag tolerance
                if (Math.Abs(p.X - Core.mouseDown.X) * 2 >= SystemParameters.MinimumHorizontalDragDistance || Math.Abs(p.Y - Core.mouseDown.Y) * 2 >= SystemParameters.MinimumVerticalDragDistance)
                {
                    Core.BeginDrag(Core.mouseDown);
                }
            }

            if (Core.IsDragging)
            {
                if (!isDragging)
                {
                    isDragging = true;
                    Debug.WriteLine("IsDragging = " + isDragging);
                    cursorBefore = Cursor;
                    Cursor = Cursors.SizeAll;
                    Mouse.Capture(this);
                }

                if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
                {
                    // ...
                }
                else
                {
                    Point p = e.GetPosition(this);

                    if (MapScaleTransform != null)
                    {
                        p = MapScaleTransform.Inverse.Transform(p);
                    }

                    p = ApplyRotationInversion(p.X, p.Y);

                    Core.mouseCurrent.X = (int)p.X;
                    Core.mouseCurrent.Y = (int)p.Y;
                    {
                        Core.Drag(Core.mouseCurrent);
                    }

                    if (IsRotated || scaleMode != ScaleModes.Integer)
                    {
                        ForceUpdateOverlays();
                    }
                    else
                    {
                        UpdateMarkersOffset();
                    }
                }
                InvalidateVisual(true);
            }
            else
            {
                if (isSelected && !selectionStart.IsEmpty && (Keyboard.Modifiers == ModifierKeys.Shift || 
                    Keyboard.Modifiers == ModifierKeys.Alt || DisableAltForSelection))
                {
                    System.Windows.Point p = e.GetPosition(this);
                    selectionEnd = FromLocalToLatLng((int)p.X, (int)p.Y);
                    {
                        PointLatLng p1 = selectionStart;
                        PointLatLng p2 = selectionEnd;

                        double x1 = Math.Min(p1.Lng, p2.Lng);
                        double y1 = Math.Max(p1.Lat, p2.Lat);
                        double x2 = Math.Max(p1.Lng, p2.Lng);
                        double y2 = Math.Min(p1.Lat, p2.Lat);

                        SelectedArea = new RectLatLng(y1, x1, x2 - x1, y1 - y2);
                    }
                }

                if (renderHelperLine)
                {
                    InvalidateVisual(true);
                }
            }
        }
        public MouseWheelZoomType MouseWheelZoomType
        {
            get
            {
                return Core.MouseWheelZoomType;
            }
            set
            {
                Core.MouseWheelZoomType = value;
            }
        }

        public bool MouseWheelZoomEnabled
        {
            get
            {
                return Core.MouseWheelZoomEnabled;
            }
            set
            {
                Core.MouseWheelZoomEnabled = value;
            }
        }
        public bool IgnoreMarkerOnMouseWheel = false;
        public bool InvertedMouseWheelZooming = false;

        System.Windows.Point ApplyRotationInversion(double x, double y)
        {
            System.Windows.Point ret = new System.Windows.Point(x, y);

            if (IsRotated)
            {
                ret = rotationMatrixInvert.Transform(ret);
            }

            return ret;
        }
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            if (MouseWheelZoomEnabled && (IsMouseDirectlyOver || IgnoreMarkerOnMouseWheel) && !Core.IsDragging)
            {
                System.Windows.Point p = e.GetPosition(this);

                if (MapScaleTransform != null)
                {
                    p = MapScaleTransform.Inverse.Transform(p);
                }

                p = ApplyRotationInversion(p.X, p.Y);

                if (Core.mouseLastZoom.X != (int)p.X && Core.mouseLastZoom.Y != (int)p.Y)
                {
                    if (MouseWheelZoomType == MouseWheelZoomType.MousePositionAndCenter)
                    {
                        Core.position = FromLocalToLatLng((int)p.X, (int)p.Y);
                    }
                    else if (MouseWheelZoomType == MouseWheelZoomType.ViewCenter)
                    {
                        Core.position = FromLocalToLatLng((int)ActualWidth / 2, (int)ActualHeight / 2);
                    }
                    else if (MouseWheelZoomType == MouseWheelZoomType.MousePositionWithoutCenter)
                    {
                        Core.position = FromLocalToLatLng((int)p.X, (int)p.Y);
                    }

                    Core.mouseLastZoom.X = (int)p.X;
                    Core.mouseLastZoom.Y = (int)p.Y;
                }

                // set mouse position to map center
                if (MouseWheelZoomType != MouseWheelZoomType.MousePositionWithoutCenter)
                {
                    System.Windows.Point ps = PointToScreen(new System.Windows.Point(ActualWidth / 2, ActualHeight / 2));
                    Stuff.SetCursorPos((int)ps.X, (int)ps.Y);
                }

                Core.MouseWheelZooming = true;

                if (e.Delta > 0)
                {
                    if (!InvertedMouseWheelZooming)
                    {
                        Zoom = ((int)Zoom) + 1;
                    }
                    else
                    {
                        Zoom = ((int)(Zoom + 0.99)) - 1;
                    }
                }
                else
                {
                    if (InvertedMouseWheelZooming)
                    {
                        Zoom = ((int)Zoom) + 1;
                    }
                    else
                    {
                        Zoom = ((int)(Zoom + 0.99)) - 1;
                    }
                }

                Core.MouseWheelZooming = false;
            }
        }

        private ScaleModes scaleMode = ScaleModes.Integer;

        public ScaleModes ScaleMode
        {
            get
            {
                return scaleMode;
            }
            set
            {
                scaleMode = value;
                InvalidateVisual();
            }
        }

        public enum ScaleModes
        {
            /// <summary>
            /// no scaling
            /// </summary>
            Integer,

            /// <summary>
            /// scales to fractional level using a stretched tiles, any issues -> http://greatmaps.codeplex.com/workitem/16046
            /// </summary>
            ScaleUp,

            /// <summary>
            /// scales to fractional level using a narrowed tiles, any issues -> http://greatmaps.codeplex.com/workitem/16046
            /// </summary>
            ScaleDown,

            /// <summary>
            /// scales to fractional level using a combination both stretched and narrowed tiles, any issues -> http://greatmaps.codeplex.com/workitem/16046
            /// </summary>
            Dynamic
        }

        public System.Windows.Point MapPoint
        {
            get
            {
                return (System.Windows.Point)GetValue(MapPointProperty);
            }
            set
            {
                SetValue(MapPointProperty, value);
            }
        }


        // Using a DependencyProperty as the backing store for point.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MapPointProperty =
               DependencyProperty.Register("MapPoint", typeof(System.Windows.Point), typeof(MiniGMapControl), 
                   new PropertyMetadata(new Point(), OnMapPointPropertyChanged));


        private static void OnMapPointPropertyChanged(DependencyObject source,
        DependencyPropertyChangedEventArgs e)
        {
            Point temp = (Point)e.NewValue;
            (source as MiniGMapControl).Position = new PointLatLng(temp.X, temp.Y);
        }


        public PointLatLng NewPoint
        {
            get { return Core.newPoint; }
            set { Core.newPoint = value; }
        }

        public PointLatLng Position
        {
            get
            {
                return Core.Position;
            }
            set
            {
                Core.Position = value;

                if (Core.IsStarted)
                {
                    ForceUpdateOverlays();
                }
            }
        }
        private RectLatLng selectedArea;

        public int MaxZoom
        {
            get
            {
                return Core.maxZoom;
            }
            set
            {
                Core.maxZoom = value;
            }
        }
        public int MinZoom
        {
            get
            {
                return Core.minZoom;
            }
            set
            {
                Core.minZoom = value;
            }
        }

        public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register("Zoom", 
            typeof(double), typeof(MiniGMapControl), new UIPropertyMetadata(0.0, 
                new PropertyChangedCallback(ZoomPropertyChanged), new CoerceValueCallback(OnCoerceZoom)));

        /// <summary>
        /// map zoom
        /// </summary>
        public double Zoom
        {
            get
            {
                return (double)(GetValue(ZoomProperty));
            }
            set
            {
                SetValue(ZoomProperty, value);
            }
        }

        private static object OnCoerceZoom(DependencyObject o, object value)
        {
            if (o is MiniGMapControl map)
            {
                double result = (double)value;
                if (result > map.MaxZoom)
                {
                    result = map.MaxZoom;
                }
                if (result < map.MinZoom)
                {
                    result = map.MinZoom;
                }

                return result;
            }
            else
            {
                return value;
            }
        }

        private static void ZoomPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MiniGMapControl map = (MiniGMapControl)d;
            if (map != null && map.MapProvider.Projection != null)
            {
                double value = (double)e.NewValue;

                Debug.WriteLine("Zoom: " + e.OldValue + " -> " + value);

                double remainder = value % 1;
                if (map.ScaleMode != ScaleModes.Integer && remainder != 0 && map.ActualWidth > 0)
                {
                    bool scaleDown;

                    switch (map.ScaleMode)
                    {
                        case ScaleModes.ScaleDown:
                            scaleDown = true;
                            break;

                        case ScaleModes.Dynamic:
                            scaleDown = remainder > 0.25;
                            break;

                        default:
                            scaleDown = false;
                            break;
                    }

                    if (scaleDown)
                        remainder--;

                    double scaleValue = Math.Pow(2d, remainder);
                    {
                        if (map.MapScaleTransform == null)
                        {
                            map.MapScaleTransform = map.lastScaleTransform;
                        }
                        map.MapScaleTransform.ScaleX = scaleValue;
                        map.MapScaleTransform.ScaleY = scaleValue;

                        map.Core.scaleX = 1 / scaleValue;
                        map.Core.scaleY = 1 / scaleValue;

                        map.MapScaleTransform.CenterX = map.ActualWidth / 2;
                        map.MapScaleTransform.CenterY = map.ActualHeight / 2;
                    }

                    map.Core.Zoom = Convert.ToInt32(scaleDown ? Math.Ceiling(value) : value - remainder);
                }
                else
                {
                    map.MapScaleTransform = null;
                    map.Core.scaleX = 1;
                    map.Core.scaleY = 1;
                    map.Core.Zoom = (int)Math.Floor(value);
                }

                if (map.IsLoaded)
                {
                    map.ForceUpdateOverlays();
                    map.InvalidateVisual(true);
                }
            }
        }

        public void InvalidateVisual(bool forced)
        {
            if (forced)
            {
                lock (Core.invalidationLock)
                {
                    Core.lastInvalidation = DateTime.Now;
                }
                base.InvalidateVisual();
            }
            else
            {
                InvalidateVisual();
            }
        }

        public RectLatLng SelectedArea
        {
            get
            {
                return selectedArea;
            }
            set
            {
                selectedArea = value;
                InvalidateVisual();
            }
        }
        public RectLatLng ViewArea
        {
            get
            {
                if (!IsRotated)
                {
                    return Core.ViewArea;
                }
                else if (Core.Provider.Projection != null)
                {
                    var p = FromLocalToLatLng(0, 0);
                    var p2 = FromLocalToLatLng((int)Width, (int)Height);

                    return RectLatLng.FromLTRB(p.Lng, p.Lat, p2.Lng, p2.Lat);
                }
                return RectLatLng.Empty;
            }
        }

        public static readonly DependencyProperty MapProviderProperty = DependencyProperty.Register("MapProvider", 
            typeof(GMapProvider), typeof(MiniGMapControl), new UIPropertyMetadata(EmptyProvider.Instance, 
                new PropertyChangedCallback(MapProviderPropertyChanged)));
        public static readonly DependencyProperty MapProvider1Property = DependencyProperty.Register("MapProvider1",
            typeof(GMapProvider), typeof(MiniGMapControl), new UIPropertyMetadata(EmptyProvider.Instance,
                new PropertyChangedCallback(MapProvider1PropertyChanged)));
        public GMapProvider MapProvider
        {
            get
            {
                return GetValue(MapProviderProperty) as GMapProvider;
            }
            set
            {
                SetValue(MapProviderProperty, value);
            }
        }
        public GMapProvider MapProvider1
        {
            get
            {
                return GetValue(MapProvider1Property) as GMapProvider;
            }
            set
            {
                SetValue(MapProvider1Property, value);
            }
        }
        private static void MapProviderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MiniGMapControl map = (MiniGMapControl)d;
            if (map != null && e.NewValue != null)
            {

                RectLatLng viewarea = map.SelectedArea;
                if (viewarea != RectLatLng.Empty)
                {
                    map.Position = new PointLatLng(viewarea.Lat - viewarea.HeightLat / 2, viewarea.Lng + viewarea.WidthLng / 2);
                }
                else
                {
                    viewarea = map.ViewArea;
                }

                map.Core.Provider = e.NewValue as GMapProvider;

                map.Copyright = null;
                if (!string.IsNullOrEmpty(map.Core.Provider.Copyright))
                {
                    map.Copyright = new FormattedText(map.Core.Provider.Copyright, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("GenericSansSerif"), 9, Brushes.Navy);
                }

                if (map.Core.IsStarted && map.Core.zoomToArea)
                {
                    // restore zoomrect as close as possible
                    if (viewarea != RectLatLng.Empty && viewarea != map.ViewArea)
                    {
                        int bestZoom = map.Core.GetMaxZoomToFitRect(viewarea);
                        if (bestZoom > 0 && map.Zoom != bestZoom)
                        {
                            map.Zoom = bestZoom;
                        }
                    }
                }
            }
        }
        private static void MapProvider1PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MiniGMapControl map = (MiniGMapControl)d;
            if (map != null && e.NewValue != null)
            {

                RectLatLng viewarea = map.SelectedArea;
                if (viewarea != RectLatLng.Empty)
                {
                    map.Position = new PointLatLng(viewarea.Lat - viewarea.HeightLat / 2, viewarea.Lng + viewarea.WidthLng / 2);
                }
                else
                {
                    viewarea = map.ViewArea;
                }

                map.Core.Provider1 = e.NewValue as GMapProvider;

                map.Copyright = null;
                if (!string.IsNullOrEmpty(map.Core.Provider1.Copyright))
                {
                    map.Copyright = new FormattedText(map.Core.Provider1.Copyright, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("GenericSansSerif"), 9, Brushes.Navy);
                }

                if (map.Core.IsStarted && map.Core.zoomToArea)
                {
                    // restore zoomrect as close as possible
                    if (viewarea != RectLatLng.Empty && viewarea != map.ViewArea)
                    {
                        int bestZoom = map.Core.GetMaxZoomToFitRect(viewarea);
                        if (bestZoom > 0 && map.Zoom != bestZoom)
                        {
                            map.Zoom = bestZoom;
                        }
                    }
                }
            }
        }
        void ForceUpdateOverlays()
        {
            ForceUpdateOverlays(ItemsSource);
        }
        void ForceUpdateOverlays(System.Collections.IEnumerable items)
        {
            using (Dispatcher.DisableProcessing())
            {
                UpdateMarkersOffset();

                foreach (GMapMarker i in items)
                {
                    if (i != null)
                    {
                        i.ForceUpdateLocalPosition(this);

                        if (i is IShapable)
                        {
                            (i as IShapable).RegenerateShape(this);
                        }
                    }
                }
            }
            InvalidateVisual();
        }
        internal readonly TranslateTransform MapOverlayTranslateTransform = new TranslateTransform();

        void UpdateMarkersOffset()
        {
            if (MapCanvas != null)
            {
                if (MapScaleTransform != null)
                {
                    var tp = MapScaleTransform.Transform(new System.Windows.Point(Core.renderOffset.X, Core.renderOffset.Y));
                    MapOverlayTranslateTransform.X = tp.X;
                    MapOverlayTranslateTransform.Y = tp.Y;

                    // map is scaled already
                    MapTranslateTransform.X = Core.renderOffset.X;
                    MapTranslateTransform.Y = Core.renderOffset.Y;
                }
                else
                {
                    MapTranslateTransform.X = Core.renderOffset.X;
                    MapTranslateTransform.Y = Core.renderOffset.Y;

                    MapOverlayTranslateTransform.X = MapTranslateTransform.X;
                    MapOverlayTranslateTransform.Y = MapTranslateTransform.Y;
                }
            }
        }
        Canvas mapCanvas = null;
        internal Canvas MapCanvas
        {
            get
            {
                if (mapCanvas == null)
                {
                    if (this.VisualChildrenCount > 0)
                    {
                        Border border = VisualTreeHelper.GetChild(this, 0) as Border;
                        ItemsPresenter items = border.Child as ItemsPresenter;
                        DependencyObject target = VisualTreeHelper.GetChild(items, 0);
                        mapCanvas = target as Canvas;

                        mapCanvas.RenderTransform = MapTranslateTransform;
                    }
                }

                return mapCanvas;
            }
        }
        public bool IsRotated
        {
            get
            {
                return Core.IsRotated;
            }
        }
        public PointLatLng FromLocalToLatLng(int x, int y)
        {
            if (MapScaleTransform != null)
            {
                var tp = MapScaleTransform.Inverse.Transform(new System.Windows.Point(x, y));
                x = (int)tp.X;
                y = (int)tp.Y;
            }

            if (IsRotated)
            {
                var f = rotationMatrixInvert.Transform(new System.Windows.Point(x, y));

                x = (int)f.X;
                y = (int)f.Y;
            }

            return Core.FromLocalToLatLng(x, y);
        }
        public GPoint FromLatLngToLocal(PointLatLng point)
        {
            GPoint ret = Core.FromLatLngToLocal(point);

            if (MapScaleTransform != null)
            {
                var tp = MapScaleTransform.Transform(new System.Windows.Point(ret.X, ret.Y));
                ret.X = (int)tp.X;
                ret.Y = (int)tp.Y;
            }

            if (IsRotated)
            {
                var f = rotationMatrix.Transform(new System.Windows.Point(ret.X, ret.Y));

                ret.X = (int)f.X;
                ret.Y = (int)f.Y;
            }

            return ret;
        }
        public event TileLoadComplete OnTileLoadComplete
        {
            add
            {
                Core.OnTileLoadComplete += value;
            }
            remove
            {
                Core.OnTileLoadComplete -= value;
            }
        }

        public event TileLoadStart OnTileLoadStart
        {
            add
            {
                Core.OnTileLoadStart += value;
            }
            remove
            {
                Core.OnTileLoadStart -= value;
            }
        }
        public GPoint PositionPixel
        {
            get
            {
                return Core.PositionPixel;
            }
        }
        public string CacheLocation
        {
            get
            {
                return CacheLocator.Location;
            }
            set
            {
                CacheLocator.Location = value;
            }
        }
        public virtual void Dispose()
        {
            if (Core.IsStarted)
            {
                Core.OnMapZoomChanged -= new MapZoomChanged(ForceUpdateOverlays);
                Loaded -= new RoutedEventHandler(GMapControl_Loaded);
                Dispatcher.ShutdownStarted -= new EventHandler(Dispatcher_ShutdownStarted);
                SizeChanged -= new SizeChangedEventHandler(GMapControl_SizeChanged);
                if (loadedApp != null)
                {
                    loadedApp.SessionEnding -= new SessionEndingCancelEventHandler(Current_SessionEnding);
                }
                Core.OnMapClose();
            }
        }
        void GMapControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Core.IsStarted)
            {
                if (lazyEvents)
                {
                    lazyEvents = false;

                    if (lazySetZoomToFitRect.HasValue)
                    {
                        SetZoomToFitRect(lazySetZoomToFitRect.Value);
                        lazySetZoomToFitRect = null;
                    }
                }
                Core.OnMapOpen().ProgressChanged += new ProgressChangedEventHandler(InvalidatorEngage);
                ForceUpdateOverlays();

                if (Application.Current != null)
                {
                    loadedApp = Application.Current;

                    loadedApp.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle,
                       new Action(delegate ()
                       {
                           loadedApp.SessionEnding += new SessionEndingCancelEventHandler(Current_SessionEnding);
                       }
                       ));
                }
            }
        }
        void InvalidatorEngage(object sender, ProgressChangedEventArgs e)
        {
            base.InvalidateVisual();
        }
        void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            Dispose();
        }
        void GMapControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            System.Windows.Size constraint = e.NewSize;

            // 50px outside control
            //region = new GRect(-50, -50, (int)constraint.Width + 100, (int)constraint.Height + 100);

            Core.OnMapSizeChanged((int)constraint.Width, (int)constraint.Height);

            if (Core.IsStarted)
            {
                if (IsRotated)
                {
                    UpdateRotationMatrix();
                }

                ForceUpdateOverlays();
            }
        }
        void UpdateRotationMatrix()
        {
            System.Windows.Point center = new System.Windows.Point(ActualWidth / 2.0, ActualHeight / 2.0);

            rotationMatrix.Angle = -Bearing;
            rotationMatrix.CenterY = center.Y;
            rotationMatrix.CenterX = center.X;

            rotationMatrixInvert = rotationMatrix.Inverse;
        }
        public float Bearing
        {
            get
            {
                return Core.bearing;
            }
            set
            {
                if (Core.bearing != value)
                {
                    bool resize = Core.bearing == 0;
                    Core.bearing = value;

                    UpdateRotationMatrix();

                    if (value != 0 && value % 360 != 0)
                    {
                        Core.IsRotated = true;

                        if (Core.tileRectBearing.Size == Core.tileRect.Size)
                        {
                            Core.tileRectBearing = Core.tileRect;
                            Core.tileRectBearing.Inflate(1, 1);
                        }
                    }
                    else
                    {
                        Core.IsRotated = false;
                        Core.tileRectBearing = Core.tileRect;
                    }

                    if (resize)
                    {
                        Core.OnMapSizeChanged((int)ActualWidth, (int)ActualHeight);
                    }

                    if (Core.IsStarted)
                    {
                        ForceUpdateOverlays();
                    }
                }
            }
        }
        void Current_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            GMaps.Instance.CancelTileCaching();
        }
        public bool SetZoomToFitRect(RectLatLng rect)
        {
            if (lazyEvents)
            {
                lazySetZoomToFitRect = rect;
            }
            else
            {
                int maxZoom = Core.GetMaxZoomToFitRect(rect);
                if (maxZoom > 0)
                {
                    PointLatLng center = new PointLatLng(rect.Lat - (rect.HeightLat / 2), rect.Lng + (rect.WidthLng / 2));
                    Position = center;

                    if (maxZoom > MaxZoom)
                    {
                        maxZoom = MaxZoom;
                    }

                    if (Core.Zoom != maxZoom)
                    {
                        Zoom = maxZoom;
                    }

                    return true;
                }
            }
            return false;
        }

        public Brush EmptyMapBackground = Brushes.WhiteSmoke;
        internal readonly TranslateTransform MapTranslateTransform = new TranslateTransform();
        readonly Pen VirtualCenterCrossPen = new Pen(Brushes.Blue, 1);
        public bool SelectionUseCircle = false;
        public Pen CenterCrossPen = new Pen(Brushes.Red, 1);
        public Pen SelectionPen = new Pen(Brushes.Blue, 2.0);
        bool renderHelperLine = false;
        public Pen HelperLinePen = new Pen(Brushes.Blue, 1);

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (!Core.IsStarted)
                return;

            drawingContext.DrawRectangle(EmptyMapBackground, null, new Rect(RenderSize));

            if (IsRotated)
            {
                drawingContext.PushTransform(rotationMatrix);

                if (MapScaleTransform != null)
                {
                    drawingContext.PushTransform(MapScaleTransform);
                    drawingContext.PushTransform(MapTranslateTransform);
                    {
                        DrawMap(drawingContext);
                    }
                    drawingContext.Pop();
                    drawingContext.Pop();
                }
                else
                {
                    drawingContext.PushTransform(MapTranslateTransform);
                    {
                        DrawMap(drawingContext);
                    }
                    drawingContext.Pop();
                }

                drawingContext.Pop();
            }
            else
            {
                if (MapScaleTransform != null)
                {
                    drawingContext.PushTransform(MapScaleTransform);
                    drawingContext.PushTransform(MapTranslateTransform);
                    {
                        DrawMap(drawingContext);


                    }
                    drawingContext.Pop();
                    drawingContext.Pop();
                }
                else
                {
                    drawingContext.PushTransform(MapTranslateTransform);
                    {
                        DrawMap(drawingContext);

                    }
                    drawingContext.Pop();
                }
            }

            // selection
            if (!SelectedArea.IsEmpty)
            {
                GPoint p1 = FromLatLngToLocal(SelectedArea.LocationTopLeft);
                GPoint p2 = FromLatLngToLocal(SelectedArea.LocationRightBottom);

                long x1 = p1.X;
                long y1 = p1.Y;
                long x2 = p2.X;
                long y2 = p2.Y;

                if (SelectionUseCircle)
                {
                    drawingContext.DrawEllipse(SelectedAreaFill, SelectionPen, new System.Windows.Point(x1 + (x2 - x1) / 2, y1 + (y2 - y1) / 2), (x2 - x1) / 2, (y2 - y1) / 2);
                }
                else
                {
                    drawingContext.DrawRoundedRectangle(SelectedAreaFill, SelectionPen, new Rect(x1, y1, x2 - x1, y2 - y1), 5, 5);
                }
            }

            if (ShowCenter)
            {
                drawingContext.DrawLine(CenterCrossPen, new System.Windows.Point((ActualWidth / 2) - 5, ActualHeight / 2), new System.Windows.Point((ActualWidth / 2) + 5, ActualHeight / 2));
                drawingContext.DrawLine(CenterCrossPen, new System.Windows.Point(ActualWidth / 2, (ActualHeight / 2) - 5), new System.Windows.Point(ActualWidth / 2, (ActualHeight / 2) + 5));
            }

            if (renderHelperLine)
            {
                var p = Mouse.GetPosition(this);

                drawingContext.DrawLine(HelperLinePen, new Point(p.X, 0), new Point(p.X, ActualHeight));
                drawingContext.DrawLine(HelperLinePen, new Point(0, p.Y), new Point(ActualWidth, p.Y));
            }

            #region -- copyright --

            if (Copyright != null)
            {
                drawingContext.DrawText(Copyright, new System.Windows.Point(5, ActualHeight - Copyright.Height - 5));
            }

            #endregion

            base.OnRender(drawingContext);
        }
        void DrawMap(DrawingContext g)
        {
            if (MapProvider == EmptyProvider.Instance || MapProvider == null)
            {
                return;
            }

            Core.tileDrawingListLock.AcquireReaderLock();
            Core.Matrix.EnterReadLock();
            try
            {
                foreach (var tilePoint in Core.tileDrawingList)
                {
                    Core.tileRect.Location = tilePoint.PosPixel;
                    Core.tileRect.OffsetNegative(Core.compensationOffset);

                    //if(region.IntersectsWith(Core.tileRect) || IsRotated)
                    {
                        bool found = false;

                        Tile t = Core.Matrix.GetTileWithNoLock(Core.Zoom, tilePoint.PosXY);
                        if (t.NotEmpty)
                        {
                            foreach (GMapImage img in t.Overlays)
                            {
                                if (img != null && img.Img != null)
                                {
                                    if (!found)
                                        found = true;

                                    var imgRect = new Rect(Core.tileRect.X + 0.6, Core.tileRect.Y + 0.6, Core.tileRect.Width + 0.6, Core.tileRect.Height + 0.6);
                                    if (!img.IsParent)
                                    {
                                        g.DrawImage(img.Img, imgRect);
                                    }
                                    else
                                    {
                                        // TODO: move calculations to loader thread
                                        var geometry = new RectangleGeometry(imgRect);
                                        var parentImgRect = new Rect(Core.tileRect.X - Core.tileRect.Width * img.Xoff + 0.6, Core.tileRect.Y - Core.tileRect.Height * img.Yoff + 0.6, Core.tileRect.Width * img.Ix + 0.6, Core.tileRect.Height * img.Ix + 0.6);

                                        g.PushClip(geometry);
                                        g.DrawImage(img.Img, parentImgRect);
                                        g.Pop();
                                        geometry = null;
                                    }
                                }
                            }
                        }
                        else if (FillEmptyTiles && MapProvider.Projection is MercatorProjection)
                        {
                            #region -- fill empty tiles --
                            int zoomOffset = 1;
                            Tile parentTile = Tile.Empty;
                            long Ix = 0;

                            while (!parentTile.NotEmpty && zoomOffset < Core.Zoom && zoomOffset <= LevelsKeepInMemmory)
                            {
                                Ix = (long)Math.Pow(2, zoomOffset);
                                parentTile = Core.Matrix.GetTileWithNoLock(Core.Zoom - zoomOffset++, new GPoint((int)(tilePoint.PosXY.X / Ix), (int)(tilePoint.PosXY.Y / Ix)));
                            }

                            if (parentTile.NotEmpty)
                            {
                                long Xoff = Math.Abs(tilePoint.PosXY.X - (parentTile.Pos.X * Ix));
                                long Yoff = Math.Abs(tilePoint.PosXY.Y - (parentTile.Pos.Y * Ix));

                                var geometry = new RectangleGeometry(new Rect(Core.tileRect.X + 0.6, Core.tileRect.Y + 0.6, Core.tileRect.Width + 0.6, Core.tileRect.Height + 0.6));
                                var parentImgRect = new Rect(Core.tileRect.X - Core.tileRect.Width * Xoff + 0.6, Core.tileRect.Y - Core.tileRect.Height * Yoff + 0.6, Core.tileRect.Width * Ix + 0.6, Core.tileRect.Height * Ix + 0.6);

                                // render tile 
                                {
                                    foreach (GMapImage img in parentTile.Overlays)
                                    {
                                        if (img != null && img.Img != null && !img.IsParent)
                                        {
                                            if (!found)
                                                found = true;

                                            g.PushClip(geometry);
                                            g.DrawImage(img.Img, parentImgRect);
                                            g.DrawRectangle(SelectedAreaFill, null, geometry.Bounds);
                                            g.Pop();
                                        }
                                    }
                                }

                                geometry = null;
                            }
                            #endregion
                        }

                        // add text if tile is missing
                        if (!found)
                        {
                            lock (Core.FailedLoads)
                            {
                                var lt = new LoadTask(tilePoint.PosXY, Core.Zoom);

                                if (Core.FailedLoads.ContainsKey(lt))
                                {
                                    g.DrawRectangle(EmptytileBrush, EmptyTileBorders, new Rect(Core.tileRect.X, Core.tileRect.Y, Core.tileRect.Width, Core.tileRect.Height));

                                    var ex = Core.FailedLoads[lt];
                                    FormattedText TileText = new FormattedText("Exception: " + ex.Message, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, tileTypeface, 14, Brushes.Red)
                                    {
                                        MaxTextWidth = Core.tileRect.Width - 11
                                    };

                                    g.DrawText(TileText, new System.Windows.Point(Core.tileRect.X + 11, Core.tileRect.Y + 11));

                                    g.DrawText(EmptyTileText, new System.Windows.Point(Core.tileRect.X + Core.tileRect.Width / 2 - EmptyTileText.Width / 2, Core.tileRect.Y + Core.tileRect.Height / 2 - EmptyTileText.Height / 2));
                                }
                            }
                        }

                        if (ShowTileGridLines)
                        {
                            g.DrawRectangle(null, EmptyTileBorders, new Rect(Core.tileRect.X, Core.tileRect.Y, Core.tileRect.Width, Core.tileRect.Height));

                            //if (tilePoint.PosXY == Core.centerTileXYLocation)
                            //{
                            //    FormattedText TileText = new FormattedText("CENTER:" + tilePoint.ToString(), System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, tileTypeface, 16, Brushes.Red)
                            //    {
                            //        MaxTextWidth = Core.tileRect.Width
                            //    };
                            //    g.DrawText(TileText, new System.Windows.Point(Core.tileRect.X + Core.tileRect.Width / 2 - EmptyTileText.Width / 2, Core.tileRect.Y + Core.tileRect.Height / 2 - TileText.Height / 2));
                            //}
                            //else
                            //{
                            //    FormattedText TileText = new FormattedText("TILE: " + tilePoint.ToString(), System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, tileTypeface, 16, Brushes.Red)
                            //    {
                            //        MaxTextWidth = Core.tileRect.Width
                            //    };
                            //    g.DrawText(TileText, new System.Windows.Point(Core.tileRect.X + Core.tileRect.Width / 2 - EmptyTileText.Width / 2, Core.tileRect.Y + Core.tileRect.Height / 2 - TileText.Height / 2));
                            //}
                        }
                    }
                }
            }
            finally
            {
                Core.Matrix.LeaveReadLock();
                Core.tileDrawingListLock.ReleaseReaderLock();
            }
        }
        public bool FillEmptyTiles
        {
            get
            {
                return Core.fillEmptyTiles;
            }
            set
            {
                Core.fillEmptyTiles = value;
            }
        }
        public int LevelsKeepInMemmory
        {
            get
            {
                return Core.LevelsKeepInMemmory;
            }

            set
            {
                Core.LevelsKeepInMemmory = value;
            }
        }
        bool showTileGridLines = false;

        public bool ShowTileGridLines
        {
            get
            {
                return showTileGridLines;
            }
            set
            {
                showTileGridLines = value;
                InvalidateVisual();
            }
        }
    }
}
