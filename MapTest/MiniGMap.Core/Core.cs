using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MiniGMap.Core
{
    internal class Core : IDisposable
    {
        public PointLatLng position;
        public GPoint positionPixel;

        public GPoint renderOffset;
        public GPoint centerTileXYLocation;
        public GPoint centerTileXYLocationLast;
        public GPoint dragPoint;
        public GPoint compensationOffset;

        public GPoint mouseDown;
        public GPoint mouseCurrent;
        public GPoint mouseLastZoom;

        public MouseWheelZoomType MouseWheelZoomType = MouseWheelZoomType.MousePositionAndCenter;
        public bool MouseWheelZoomEnabled = true;

        public PointLatLng? LastLocationInBounds = null;
        public bool VirtualSizeEnabled = false;

        public GSize sizeOfMapArea;
        public GSize minOfTiles;
        public GSize maxOfTiles;

        public GRect tileRect;
        public GRect tileRectBearing;
        //public GRect currentRegion;
        public float bearing = 0;
        public bool IsRotated = false;

        public bool fillEmptyTiles = true;

        public TileMatrix Matrix = new TileMatrix();

        public List<DrawTile> tileDrawingList = new List<DrawTile>();
        public FastReaderWriterLock tileDrawingListLock = new FastReaderWriterLock();

        DateTime LastTileLoadStart = DateTime.Now;
        DateTime LastTileLoadEnd = DateTime.Now;
        internal volatile bool IsStarted = false;
        int zoom;

        internal double scaleX = 1;
        internal double scaleY = 1;

        internal int maxZoom = 2;
        internal int minZoom = 2;
        internal int Width;
        internal int Height;

        internal int pxRes100m;  // 100 meters
        internal int pxRes1000m;  // 1km  
        internal int pxRes10km; // 10km
        internal int pxRes100km; // 100km
        internal int pxRes1000km; // 1000km
        internal int pxRes5000km; // 5000km
        public bool IsDragging = false;
        public int LevelsKeepInMemmory = 5;
        internal Dictionary<LoadTask, Exception> FailedLoads = new Dictionary<LoadTask, Exception>(new LoadTaskComparer());
        bool RaiseEmptyTileError = false;
        public bool MouseWheelZooming = false;
        public bool updatingBounds = false;
        public event MapZoomChanged OnMapZoomChanged;
        internal bool zoomToArea = true;
        public event MapTypeChanged OnMapTypeChanged;
        public event PositionChanged OnCurrentPositionChanged;
        static int loadWaitCount = 0;
        public event TileLoadStart OnTileLoadStart;
        volatile int okZoom = 0;
        volatile int skipOverZoom = 0;
        public AutoResetEvent Refresh = new AutoResetEvent(false);
        static List<Task> tileLoadQueue4Tasks;
        static readonly BlockingCollection<LoadTask> tileLoadQueue4 = new BlockingCollection<LoadTask>(new ConcurrentStack<LoadTask>());
        static readonly int GThreadPoolSize = 4;
        public event MapDrag OnMapDrag;
        public event TileLoadComplete OnTileLoadComplete;
        public event EmptyTileError OnEmptyTileError;
        public int RetryLoadTile = 0;
        BackgroundWorker invalidator;
        internal static int instances = 0;
        internal readonly object invalidationLock = new object();
        internal DateTime lastInvalidation = DateTime.Now;
        internal string SystemType;
        public RenderMode RenderMode = RenderMode.WPF;
        public bool CanDragMap = true;
        public void EndDrag()
        {
            IsDragging = false;
            mouseDown = GPoint.Empty;

            Refresh.Set();
        }
        public void BeginDrag(GPoint pt)
        {
            dragPoint.X = pt.X - renderOffset.X;
            dragPoint.Y = pt.Y - renderOffset.Y;
            IsDragging = true;
        }
        public int Zoom
        {
            get
            {
                return zoom;
            }
            set
            {
                if (zoom != value && !IsDragging)
                {
                    zoom = value;

                    minOfTiles = Provider.Projection.GetTileMatrixMinXY(value);
                    maxOfTiles = Provider.Projection.GetTileMatrixMaxXY(value);

                    positionPixel = Provider.Projection.FromLatLngToPixel(Position, value);

                    if (IsStarted)
                    {
                        CancelAsyncTasks();

                        Matrix.ClearLevelsBelove(zoom - LevelsKeepInMemmory);
                        Matrix.ClearLevelsAbove(zoom + LevelsKeepInMemmory);

                        lock (FailedLoads)
                        {
                            FailedLoads.Clear();
                            RaiseEmptyTileError = true;
                        }

                        GoToCurrentPositionOnZoom();
                        UpdateBounds();

                        OnMapZoomChanged?.Invoke();
                    }
                }
            }
        }

        public GMapProvider provider;
        public GMapProvider Provider
        {
            get
            {
                return provider;
            }
            set
            {
                if (provider == null || !provider.Equals(value))
                {
                    bool diffProjection = (provider == null || provider.Projection != value.Projection);

                    provider = value;

                    if (!provider.IsInitialized)
                    {
                        provider.IsInitialized = true;
                        provider.OnInitialized();
                    }

                    if (provider.Projection != null && diffProjection)
                    {
                        tileRect = new GRect(GPoint.Empty, Provider.Projection.TileSize);
                        tileRectBearing = tileRect;
                        if (IsRotated)
                        {
                            tileRectBearing.Inflate(1, 1);
                        }

                        minOfTiles = Provider.Projection.GetTileMatrixMinXY(Zoom);
                        maxOfTiles = Provider.Projection.GetTileMatrixMaxXY(Zoom);
                        positionPixel = Provider.Projection.FromLatLngToPixel(Position, Zoom);
                    }

                    if (IsStarted)
                    {
                        CancelAsyncTasks();
                        if (diffProjection)
                        {
                            OnMapSizeChanged(Width, Height);
                        }
                        ReloadMap();

                        if (minZoom < provider.MinZoom)
                        {
                            minZoom = provider.MinZoom;
                        }

                        //if(provider.MaxZoom.HasValue && maxZoom > provider.MaxZoom)
                        //{
                        //   maxZoom = provider.MaxZoom.Value;
                        //}

                        zoomToArea = true;

                        if (provider.Area.HasValue && !provider.Area.Value.Contains(Position))
                        {
                            SetZoomToFitRect(provider.Area.Value);
                            zoomToArea = false;
                        }

                        OnMapTypeChanged?.Invoke(value);
                    }
                }
            }
        }

        public PointLatLng Position
        {
            get
            {

                return position;
            }
            set
            {
                position = value;
                positionPixel = Provider.Projection.FromLatLngToPixel(value, Zoom);

                if (IsStarted)
                {
                    if (!IsDragging)
                    {
                        GoToCurrentPosition();
                    }

                    OnCurrentPositionChanged?.Invoke(position);
                }
            }
        }
        public GPoint PositionPixel
        {
            get
            {
                return positionPixel;
            }
        }
        public RectLatLng ViewArea
        {
            get
            {
                if (Provider.Projection != null)
                {
                    var p = FromLocalToLatLng(0, 0);
                    var p2 = FromLocalToLatLng(Width, Height);

                    return RectLatLng.FromLTRB(p.Lng, p.Lat, p2.Lng, p2.Lat);
                }
                return RectLatLng.Empty;
            }
        }
        public Core()
        {
            Provider = EmptyProvider.Instance;
        }
        ~Core()
        {
            Dispose(false);
        }
        public void CancelAsyncTasks()
        {
            if (IsStarted)
            {

                //TODO: clear loading

            }
        }
        internal void GoToCurrentPositionOnZoom()
        {
            compensationOffset = positionPixel; // TODO: fix

            // reset stuff
            renderOffset = GPoint.Empty;
            dragPoint = GPoint.Empty;

            // goto location and centering
            if (MouseWheelZooming)
            {
                if (MouseWheelZoomType != MouseWheelZoomType.MousePositionWithoutCenter)
                {
                    GPoint pt = new GPoint(-(positionPixel.X - Width / 2), -(positionPixel.Y - Height / 2));
                    pt.Offset(compensationOffset);
                    renderOffset.X = pt.X - dragPoint.X;
                    renderOffset.Y = pt.Y - dragPoint.Y;
                }
                else // without centering
                {
                    renderOffset.X = -positionPixel.X - dragPoint.X;
                    renderOffset.Y = -positionPixel.Y - dragPoint.Y;
                    renderOffset.Offset(mouseLastZoom);
                    renderOffset.Offset(compensationOffset);
                }
            }
            else // use current map center
            {
                mouseLastZoom = GPoint.Empty;

                GPoint pt = new GPoint(-(positionPixel.X - Width / 2), -(positionPixel.Y - Height / 2));
                pt.Offset(compensationOffset);
                renderOffset.X = pt.X - dragPoint.X;
                renderOffset.Y = pt.Y - dragPoint.Y;
            }

            UpdateCenterTileXYLocation();
        }
        public void UpdateCenterTileXYLocation()
        {
            PointLatLng center = FromLocalToLatLng(Width / 2, Height / 2);
            GPoint centerPixel = Provider.Projection.FromLatLngToPixel(center, Zoom);
            centerTileXYLocation = Provider.Projection.FromPixelToTileXY(centerPixel);
        }
        public PointLatLng FromLocalToLatLng(long x, long y)
        {
            GPoint p = new GPoint(x, y);
            p.OffsetNegative(renderOffset);
            p.Offset(compensationOffset);

            return Provider.Projection.FromPixelToLatLng(p, Zoom);
        }
        void UpdateBounds()
        {
            if (!IsStarted || Provider.Equals(EmptyProvider.Instance))
            {
                return;
            }

            updatingBounds = true;

            tileDrawingListLock.AcquireWriterLock();
            try
            {
                #region -- find tiles around --
                tileDrawingList.Clear();

                for (long i = (int)Math.Floor(-sizeOfMapArea.Width * scaleX), countI = (int)Math.Ceiling(sizeOfMapArea.Width * scaleX); i <= countI; i++)
                {
                    for (long j = (int)Math.Floor(-sizeOfMapArea.Height * scaleY), countJ = (int)Math.Ceiling(sizeOfMapArea.Height * scaleY); j <= countJ; j++)
                    {
                        GPoint p = centerTileXYLocation;
                        p.X += i;
                        p.Y += j;



                        if (p.X >= minOfTiles.Width && p.Y >= minOfTiles.Height && p.X <= maxOfTiles.Width && p.Y <= maxOfTiles.Height)
                        {
                            DrawTile dt = new DrawTile()
                            {
                                PosXY = p,
                                PosPixel = new GPoint(p.X * tileRect.Width, p.Y * tileRect.Height),
                                DistanceSqr = (centerTileXYLocation.X - p.X) * (centerTileXYLocation.X - p.X) + (centerTileXYLocation.Y - p.Y) * (centerTileXYLocation.Y - p.Y)
                            };

                            if (!tileDrawingList.Contains(dt))
                            {
                                tileDrawingList.Add(dt);
                            }
                        }
                    }
                }

                if (GMaps.Instance.ShuffleTilesOnLoad)
                {
                    Stuff.Shuffle<DrawTile>(tileDrawingList);
                }
                else
                {
                    tileDrawingList.Sort();
                }
                #endregion
            }
            finally
            {
                tileDrawingListLock.ReleaseWriterLock();
            }


            Interlocked.Exchange(ref loadWaitCount, 0);

            tileDrawingListLock.AcquireReaderLock();
            try
            {
                foreach (DrawTile p in tileDrawingList)
                {
                    LoadTask task = new LoadTask(p.PosXY, Zoom, this);

                    AddLoadTask(task);

                }
            }
            finally
            {
                tileDrawingListLock.ReleaseReaderLock();
            }


            {
                LastTileLoadStart = DateTime.Now;
                Debug.WriteLine("OnTileLoadStart - at zoom " + Zoom + ", time: " + LastTileLoadStart.TimeOfDay);
            }

            updatingBounds = false;

            OnTileLoadStart?.Invoke();
        }
        public void OnMapSizeChanged(int width, int height)
        {
            this.Width = width;
            this.Height = height;

            if (IsRotated)
            {

                int diag = (int)Math.Round(Math.Sqrt(Width * Width + Height * Height) / Provider.Projection.TileSize.Width, MidpointRounding.AwayFromZero);

                sizeOfMapArea.Width = 1 + (diag / 2);
                sizeOfMapArea.Height = 1 + (diag / 2);
            }
            else
            {
                sizeOfMapArea.Width = 1 + (Width / Provider.Projection.TileSize.Width) / 2;
                sizeOfMapArea.Height = 1 + (Height / Provider.Projection.TileSize.Height) / 2;
            }

            Debug.WriteLine("OnMapSizeChanged, w: " + width + ", h: " + height + ", size: " + sizeOfMapArea);

            if (IsStarted)
            {
                UpdateBounds();
                GoToCurrentPosition();
            }
        }
        public void ReloadMap()
        {
            if (IsStarted)
            {
                Debug.WriteLine("------------------");

                okZoom = 0;
                skipOverZoom = 0;

                CancelAsyncTasks();

                Matrix.ClearAllLevels();

                lock (FailedLoads)
                {
                    FailedLoads.Clear();
                    RaiseEmptyTileError = true;
                }

                Refresh.Set();

                UpdateBounds();
            }
            else
            {
                throw new Exception("Please, do not call ReloadMap before form is loaded, it's useless");
            }
        }
        public bool SetZoomToFitRect(RectLatLng rect)
        {
            int mmaxZoom = GetMaxZoomToFitRect(rect);
            if (mmaxZoom > 0)
            {
                PointLatLng center = new PointLatLng(rect.Lat - (rect.HeightLat / 2), rect.Lng + (rect.WidthLng / 2));
                Position = center;

                if (mmaxZoom > maxZoom)
                {
                    mmaxZoom = maxZoom;
                }

                if (Zoom != mmaxZoom)
                {
                    Zoom = (int)mmaxZoom;
                }

                return true;
            }
            return false;
        }
        public void GoToCurrentPosition()
        {
            compensationOffset = positionPixel; // TODO: fix

            // reset stuff
            renderOffset = GPoint.Empty;
            dragPoint = GPoint.Empty;

            //var dd = new GPoint(-(CurrentPositionGPixel.X - Width / 2), -(CurrentPositionGPixel.Y - Height / 2));
            //dd.Offset(compensationOffset);

            var d = new GPoint(Width / 2, Height / 2);

            this.Drag(d);
        }
        void AddLoadTask(LoadTask t)
        {
            if (tileLoadQueue4Tasks == null)
            {
                lock (tileLoadQueue4)
                {
                    if (tileLoadQueue4Tasks == null)
                    {
                        tileLoadQueue4Tasks = new List<Task>();

                        while (tileLoadQueue4Tasks.Count < GThreadPoolSize)
                        {
                            Debug.WriteLine("creating ProcessLoadTask: " + tileLoadQueue4Tasks.Count);

                            tileLoadQueue4Tasks.Add(Task.Factory.StartNew(delegate ()
                            {
                                string ctid = "ProcessLoadTask[" + Thread.CurrentThread.ManagedThreadId + "]";
                                Thread.CurrentThread.Name = ctid;

                                Debug.WriteLine(ctid + ": started");
                                do
                                {
                                    if (tileLoadQueue4.Count == 0)
                                    {
                                        Debug.WriteLine(ctid + ": ready");

                                        if (Interlocked.Increment(ref loadWaitCount) >= GThreadPoolSize)
                                        {
                                            Interlocked.Exchange(ref loadWaitCount, 0);
                                            OnLoadComplete(ctid);
                                        }
                                    }
                                    ProcessLoadTask(tileLoadQueue4.Take(), ctid);
                                }
                                while (!tileLoadQueue4.IsAddingCompleted);

                                Debug.WriteLine(ctid + ": exit");

                            }, TaskCreationOptions.LongRunning));
                        }
                    }
                }
            }
            tileLoadQueue4.Add(t);
        }
        public int GetMaxZoomToFitRect(RectLatLng rect)
        {
            int zoom = minZoom;

            if (rect.HeightLat == 0 || rect.WidthLng == 0)
            {
                zoom = maxZoom / 2;
            }
            else
            {
                for (int i = (int)zoom; i <= maxZoom; i++)
                {
                    GPoint p1 = Provider.Projection.FromLatLngToPixel(rect.LocationTopLeft, i);
                    GPoint p2 = Provider.Projection.FromLatLngToPixel(rect.LocationRightBottom, i);

                    if (((p2.X - p1.X) <= Width + 10) && (p2.Y - p1.Y) <= Height + 10)
                    {
                        zoom = i;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return zoom;
        }
        public void Drag(GPoint pt)
        {
            renderOffset.X = pt.X - dragPoint.X;
            renderOffset.Y = pt.Y - dragPoint.Y;

            UpdateCenterTileXYLocation();

            if (centerTileXYLocation != centerTileXYLocationLast)
            {
                centerTileXYLocationLast = centerTileXYLocation;
                UpdateBounds();
            }

            if (IsDragging)
            {
                LastLocationInBounds = Position;
                Position = FromLocalToLatLng((int)Width / 2, (int)Height / 2);

                OnMapDrag?.Invoke();
            }
        }
        void OnLoadComplete(string ctid)
        {
            LastTileLoadEnd = DateTime.Now;
            long lastTileLoadTimeMs = (long)(LastTileLoadEnd - LastTileLoadStart).TotalMilliseconds;

            #region -- clear stuff--
            if (IsStarted)
            {
                GMaps.Instance.MemoryCache.RemoveOverload();

                tileDrawingListLock.AcquireReaderLock();
                try
                {
                    Matrix.ClearLevelAndPointsNotIn(Zoom, tileDrawingList);
                }
                finally
                {
                    tileDrawingListLock.ReleaseReaderLock();
                }
            }
            #endregion

            UpdateGroundResolution();

            Debug.WriteLine(ctid + " - OnTileLoadComplete: " + lastTileLoadTimeMs + "ms, MemoryCacheSize: " + GMaps.Instance.MemoryCache.Size + "MB");

            OnTileLoadComplete?.Invoke(lastTileLoadTimeMs);
        }
        static void ProcessLoadTask(LoadTask task, string ctid)
        {
            try
            {
                #region -- execute --

                var m = task.Core.Matrix.GetTileWithReadLock(task.Zoom, task.Pos);
                if (!m.NotEmpty)
                {
                    Debug.WriteLine(ctid + " - try load: " + task);

                    Tile t = new Tile(task.Zoom, task.Pos);

                    foreach (var tl in task.Core.provider.Overlays)
                    {
                        int retry = 0;
                        do
                        {
                            PureImage img = null;
                            Exception ex = null;

                            if (task.Zoom >= task.Core.provider.MinZoom && (!task.Core.provider.MaxZoom.HasValue || task.Zoom <= task.Core.provider.MaxZoom))
                            {
                                if (task.Core.skipOverZoom == 0 || task.Zoom <= task.Core.skipOverZoom)
                                {
                                    // tile number inversion(BottomLeft -> TopLeft)
                                    if (tl.InvertedAxisY)
                                    {
                                        img = GMaps.Instance.GetImageFrom(tl, new GPoint(task.Pos.X, task.Core.maxOfTiles.Height - task.Pos.Y), task.Zoom, out ex);
                                    }
                                    else // ok
                                    {
                                        img = GMaps.Instance.GetImageFrom(tl, task.Pos, task.Zoom, out ex);
                                    }
                                }
                            }

                            if (img != null && ex == null)
                            {
                                if (task.Core.okZoom < task.Zoom)
                                {
                                    task.Core.okZoom = task.Zoom;
                                    task.Core.skipOverZoom = 0;
                                    Debug.WriteLine("skipOverZoom disabled, okZoom: " + task.Core.okZoom);
                                }
                            }
                            else if (ex != null)
                            {
                                if ((task.Core.skipOverZoom != task.Core.okZoom) && (task.Zoom > task.Core.okZoom))
                                {
                                    if (ex.Message.Contains("(404) Not Found"))
                                    {
                                        task.Core.skipOverZoom = task.Core.okZoom;
                                        Debug.WriteLine("skipOverZoom enabled: " + task.Core.skipOverZoom);
                                    }
                                }
                            }

                            // check for parent tiles if not found
                            if (img == null && task.Core.okZoom > 0 && task.Core.fillEmptyTiles && task.Core.Provider.Projection is MercatorProjection)
                            {
                                int zoomOffset = task.Zoom > task.Core.okZoom ? task.Zoom - task.Core.okZoom : 1;
                                long Ix = 0;
                                GPoint parentTile = GPoint.Empty;

                                while (img == null && zoomOffset < task.Zoom)
                                {
                                    Ix = (long)Math.Pow(2, zoomOffset);
                                    parentTile = new GPoint((task.Pos.X / Ix), (task.Pos.Y / Ix));
                                    img = GMaps.Instance.GetImageFrom(tl, parentTile, task.Zoom - zoomOffset++, out ex);
                                }

                                if (img != null)
                                {
                                    // offsets in quadrant
                                    long Xoff = Math.Abs(task.Pos.X - (parentTile.X * Ix));
                                    long Yoff = Math.Abs(task.Pos.Y - (parentTile.Y * Ix));

                                    img.IsParent = true;
                                    img.Ix = Ix;
                                    img.Xoff = Xoff;
                                    img.Yoff = Yoff;

                                    // wpf
                                    //var geometry = new RectangleGeometry(new Rect(Core.tileRect.X + 0.6, Core.tileRect.Y + 0.6, Core.tileRect.Width + 0.6, Core.tileRect.Height + 0.6));
                                    //var parentImgRect = new Rect(Core.tileRect.X - Core.tileRect.Width * Xoff + 0.6, Core.tileRect.Y - Core.tileRect.Height * Yoff + 0.6, Core.tileRect.Width * Ix + 0.6, Core.tileRect.Height * Ix + 0.6);

                                    // gdi+
                                    //System.Drawing.Rectangle dst = new System.Drawing.Rectangle((int)Core.tileRect.X, (int)Core.tileRect.Y, (int)Core.tileRect.Width, (int)Core.tileRect.Height);
                                    //System.Drawing.RectangleF srcRect = new System.Drawing.RectangleF((float)(Xoff * (img.Img.Width / Ix)), (float)(Yoff * (img.Img.Height / Ix)), (img.Img.Width / Ix), (img.Img.Height / Ix));
                                }
                            }

                            if (img != null)
                            {
                                Debug.WriteLine(ctid + " - tile loaded: " + img.Data.Length / 1024 + "KB, " + task);
                                {
                                    t.AddOverlay(img);
                                }
                                break;
                            }
                            else
                            {
                                if (ex != null)
                                {
                                    lock (task.Core.FailedLoads)
                                    {
                                        if (!task.Core.FailedLoads.ContainsKey(task))
                                        {
                                            task.Core.FailedLoads.Add(task, ex);

                                            if (task.Core.OnEmptyTileError != null)
                                            {
                                                if (!task.Core.RaiseEmptyTileError)
                                                {
                                                    task.Core.RaiseEmptyTileError = true;
                                                    task.Core.OnEmptyTileError(task.Zoom, task.Pos);
                                                }
                                            }
                                        }
                                    }
                                }

                                if (task.Core.RetryLoadTile > 0)
                                {
                                    Debug.WriteLine(ctid + " - ProcessLoadTask: " + task + " -> empty tile, retry " + retry);
                                    {
                                        Thread.Sleep(1111);
                                    }
                                }
                            }
                        }
                        while (++retry < task.Core.RetryLoadTile);
                    }

                    if (t.HasAnyOverlays && task.Core.IsStarted)
                    {
                        task.Core.Matrix.SetTile(t);
                    }
                    else
                    {
                        t.Dispose();
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ctid + " - ProcessLoadTask: " + ex.ToString());
            }
            finally
            {
                if (task.Core.Refresh != null)
                {
                    task.Core.Refresh.Set();
                }
            }
        }
        void UpdateGroundResolution()
        {
            double rez = Provider.Projection.GetGroundResolution(Zoom, Position.Lat);
            pxRes100m = (int)(100.0 / rez); // 100 meters
            pxRes1000m = (int)(1000.0 / rez); // 1km  
            pxRes10km = (int)(10000.0 / rez); // 10km
            pxRes100km = (int)(100000.0 / rez); // 100km
            pxRes1000km = (int)(1000000.0 / rez); // 1000km
            pxRes5000km = (int)(5000000.0 / rez); // 5000km
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        void Dispose(bool disposing)
        {
            if (IsStarted)
            {
                if (invalidator != null)
                {
                    invalidator.CancelAsync();
                    invalidator.DoWork -= new DoWorkEventHandler(InvalidatorWatch);
                    invalidator.Dispose();
                    invalidator = null;
                }

                if (Refresh != null)
                {
                    Refresh.Set();
                    Refresh.Close();
                    Refresh = null;
                }

                int x = Interlocked.Decrement(ref instances);
                Debug.WriteLine("OnMapClose: " + x);

                CancelAsyncTasks();
                IsStarted = false;

                if (Matrix != null)
                {
                    Matrix.Dispose();
                    Matrix = null;
                }

                if (FailedLoads != null)
                {
                    lock (FailedLoads)
                    {
                        FailedLoads.Clear();
                        RaiseEmptyTileError = false;
                    }
                    FailedLoads = null;
                }

                tileDrawingListLock.AcquireWriterLock();
                try
                {
                    tileDrawingList.Clear();
                }
                finally
                {
                    tileDrawingListLock.ReleaseWriterLock();
                }


                //TODO: maybe



                if (tileDrawingListLock != null)
                {
                    tileDrawingListLock.Dispose();
                    tileDrawingListLock = null;
                    tileDrawingList = null;
                }

                if (x == 0)
                {
#if DEBUG
                    GMaps.Instance.CancelTileCaching();
#endif
                    GMaps.Instance.noMapInstances = true;
                    GMaps.Instance.WaitForCache.Set();
                    if (disposing)
                    {
                        GMaps.Instance.MemoryCache.Clear();
                    }
                }
            }
        }
        void InvalidatorWatch(object sender, DoWorkEventArgs e)
        {
            var w = sender as BackgroundWorker;

            TimeSpan span = TimeSpan.FromMilliseconds(111);
            int spanMs = (int)span.TotalMilliseconds;
            bool skiped = false;
            TimeSpan delta;
            DateTime now = DateTime.Now;

            while (Refresh != null && (!skiped && Refresh.WaitOne() || (Refresh.WaitOne(spanMs, false) || true)))
            {
                if (w.CancellationPending)
                    break;

                now = DateTime.Now;
                lock (invalidationLock)
                {
                    delta = now - lastInvalidation;
                }

                if (delta > span)
                {
                    lock (invalidationLock)
                    {
                        lastInvalidation = now;
                    }
                    skiped = false;

                    w.ReportProgress(1);
                    Debug.WriteLine("Invalidate delta: " + (int)delta.TotalMilliseconds + "ms");
                }
                else
                {
                    skiped = true;
                }
            }
        }
        public GPoint FromLatLngToLocal(PointLatLng latlng)
        {
            GPoint pLocal = Provider.Projection.FromLatLngToPixel(latlng, Zoom);
            pLocal.Offset(renderOffset);
            pLocal.OffsetNegative(compensationOffset);
            return pLocal;
        }
        public void OnMapClose()
        {
            Dispose();
        }
        public BackgroundWorker OnMapOpen()
        {
            if (!IsStarted)
            {
                int x = Interlocked.Increment(ref instances);
                Debug.WriteLine("OnMapOpen: " + x);

                IsStarted = true;

                if (x == 1)
                {
                    GMaps.Instance.noMapInstances = false;
                }

                GoToCurrentPosition();

                invalidator = new BackgroundWorker
                {
                    WorkerSupportsCancellation = true,
                    WorkerReportsProgress = true
                };
                invalidator.DoWork += new DoWorkEventHandler(InvalidatorWatch);
                invalidator.RunWorkerAsync();

                //if(x == 1)
                //{
                // first control shown
                //}
            }
            return invalidator;
        }
    }
}
