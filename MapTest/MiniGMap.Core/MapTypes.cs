using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;

namespace MiniGMap.Core
{
    #region enums
    public enum AccessMode
    {
        /// <summary>
        /// access only server
        /// </summary>
        ServerOnly,

        /// <summary>
        /// access first server and caches localy
        /// </summary>
        ServerAndCache,

        /// <summary>
        /// access only cache
        /// </summary>
        CacheOnly,
    }
    internal enum CacheUsage
    {
        First = 2,
        Second = 4,
        Both = First | Second
    }
    public enum LanguageType
    {
        [Description("ar")]
        Arabic,

        [Description("bg")]
        Bulgarian,

        [Description("bn")]
        Bengali,

        [Description("ca")]
        Catalan,

        [Description("cs")]
        Czech,

        [Description("da")]
        Danish,

        [Description("de")]
        German,

        [Description("el")]
        Greek,

        [Description("en")]
        English,

        [Description("en-AU")]
        EnglishAustralian,

        [Description("en-GB")]
        EnglishGreatBritain,

        [Description("es")]
        Spanish,

        [Description("eu")]
        Basque,

        [Description("fa")]
        FARSI,

        [Description("fi")]
        Finnish,

        [Description("fil")]
        Filipino,

        [Description("fr")]
        French,

        [Description("gl")]
        Galician,

        [Description("gu")]
        Gujarati,
        [Description("hi")]
        Hindi,

        [Description("hr")]
        Croatian,

        [Description("hu")]
        Hungarian,

        [Description("id")]
        Indonesian,

        [Description("it")]
        Italian,

        [Description("iw")]
        Hebrew,

        [Description("ja")]
        Japanese,

        [Description("kn")]
        Kannada,

        [Description("ko")]
        Korean,

        [Description("lt")]
        Lithuanian,

        [Description("lv")]
        Latvian,

        [Description("ml")]
        Malayalam,

        [Description("mr")]
        Marathi,

        [Description("nl")]
        Dutch,

        [Description("nn")]
        NorwegianNynorsk,

        [Description("no")]
        Norwegian,

        [Description("or")]
        Oriya,

        [Description("pl")]
        Polish,

        [Description("pt")]
        Portuguese,

        [Description("pt-BR")]
        PortugueseBrazil,

        [Description("pt-PT")]
        PortuguesePortugal,

        [Description("rm")]
        Romansch,
        [Description("ro")]
        Romanian,

        [Description("ru")]
        Russian,

        [Description("sk")]
        Slovak,

        [Description("sl")]
        Slovenian,

        [Description("sr")]
        Serbian,

        [Description("sv")]
        Swedish,

        [Description("tl")]
        TAGALOG,

        [Description("ta")]
        Tamil,

        [Description("te")]
        Telugu,

        [Description("th")]
        Thai,

        [Description("tr")]
        Turkish,

        [Description("uk")]
        Ukrainian,

        [Description("vi")]
        Vietnamese,

        [Description("zh-CN")]
        ChineseSimplified,

        [Description("zh-TW")]
        ChineseTraditional,
    }
    public enum MouseWheelZoomType
    {
        /// <summary>
        /// zooms map to current mouse position and makes it map center
        /// </summary>
        MousePositionAndCenter,

        /// <summary>
        /// zooms to current mouse position, but doesn't make it map center,
        /// google/bing style ;}
        /// </summary>
        MousePositionWithoutCenter,

        /// <summary>
        /// zooms map to current view center
        /// </summary>
        ViewCenter,
    }
    public enum RenderMode
    {
        /// <summary>
        /// gdi+ should work anywhere on Windows Forms
        /// </summary>
        GDI_PLUS,

        /// <summary>
        /// only on Windows Presentation Foundation
        /// </summary>
        WPF,
    }
    #endregion

    #region structs
    internal struct CacheQueueItem
    {
        public RawTile Tile;
        public byte[] Img;
        public CacheUsage CacheType;

        public CacheQueueItem(RawTile tile, byte[] Img, CacheUsage cacheType)
        {
            this.Tile = tile;
            this.Img = Img;
            this.CacheType = cacheType;
        }

        public override string ToString()
        {
            return Tile + ", CacheType:" + CacheType;
        }

        public void Clear()
        {
            Img = null;
        }
    }
    internal struct DrawTile : IEquatable<DrawTile>, IComparable<DrawTile>
    {
        public GPoint PosXY;
        public GPoint PosPixel;
        public double DistanceSqr;

        public override string ToString()
        {
            return PosXY + ", px: " + PosPixel;
        }

        #region IEquatable<DrawTile> Members

        public bool Equals(DrawTile other)
        {
            return (PosXY == other.PosXY);
        }

        #endregion

        #region IComparable<DrawTile> Members

        public int CompareTo(DrawTile other)
        {
            return other.DistanceSqr.CompareTo(DistanceSqr);
        }

        #endregion
    }
    public struct GRect
    {
        public static readonly GRect Empty = new GRect();

        private long x;
        private long y;
        private long width;
        private long height;

        public GRect(long x, long y, long width, long height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public GRect(GPoint location, GSize size)
        {
            this.x = location.X;
            this.y = location.Y;
            this.width = size.Width;
            this.height = size.Height;
        }

        public static GRect FromLTRB(int left, int top, int right, int bottom)
        {
            return new GRect(left,
                                 top,
                                 right - left,
                                 bottom - top);
        }

        public GPoint Location
        {
            get
            {
                return new GPoint(X, Y);
            }
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public GPoint RightBottom
        {
            get
            {
                return new GPoint(Right, Bottom);
            }
        }

        public GPoint RightTop
        {
            get
            {
                return new GPoint(Right, Top);
            }
        }

        public GPoint LeftBottom
        {
            get
            {
                return new GPoint(Left, Bottom);
            }
        }

        public GSize Size
        {
            get
            {
                return new GSize(Width, Height);
            }
            set
            {
                this.Width = value.Width;
                this.Height = value.Height;
            }
        }

        public long X
        {
            get
            {
                return x;
            }
            set
            {
                x = value;
            }
        }

        public long Y
        {
            get
            {
                return y;
            }
            set
            {
                y = value;
            }
        }

        public long Width
        {
            get
            {
                return width;
            }
            set
            {
                width = value;
            }
        }

        public long Height
        {
            get
            {
                return height;
            }
            set
            {
                height = value;
            }
        }

        public long Left
        {
            get
            {
                return X;
            }
        }

        public long Top
        {
            get
            {
                return Y;
            }
        }

        public long Right
        {
            get
            {
                return X + Width;
            }
        }

        public long Bottom
        {
            get
            {
                return Y + Height;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return height == 0 && width == 0 && x == 0 && y == 0;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GRect))
                return false;

            GRect comp = (GRect)obj;

            return (comp.X == this.X) &&
               (comp.Y == this.Y) &&
               (comp.Width == this.Width) &&
               (comp.Height == this.Height);
        }

        public static bool operator ==(GRect left, GRect right)
        {
            return (left.X == right.X
                       && left.Y == right.Y
                       && left.Width == right.Width
                       && left.Height == right.Height);
        }

        public static bool operator !=(GRect left, GRect right)
        {
            return !(left == right);
        }

        public bool Contains(long x, long y)
        {
            return this.X <= x &&
               x < this.X + this.Width &&
               this.Y <= y &&
               y < this.Y + this.Height;
        }

        public bool Contains(GPoint pt)
        {
            return Contains(pt.X, pt.Y);
        }

        public bool Contains(GRect rect)
        {
            return (this.X <= rect.X) &&
               ((rect.X + rect.Width) <= (this.X + this.Width)) &&
               (this.Y <= rect.Y) &&
               ((rect.Y + rect.Height) <= (this.Y + this.Height));
        }

        public override int GetHashCode()
        {
            if (this.IsEmpty)
            {
                return 0;
            }
            return (int)(((this.X ^ ((this.Y << 13) | (this.Y >> 0x13))) ^ ((this.Width << 0x1a) | (this.Width >> 6))) ^ ((this.Height << 7) | (this.Height >> 0x19)));
        }

        public void Inflate(long width, long height)
        {
            this.X -= width;
            this.Y -= height;
            this.Width += 2 * width;
            this.Height += 2 * height;
        }

        public void Inflate(GSize size)
        {
            Inflate(size.Width, size.Height);
        }

        public static GRect Inflate(GRect rect, long x, long y)
        {
            GRect r = rect;
            r.Inflate(x, y);
            return r;
        }

        public void Intersect(GRect rect)
        {
            GRect result = GRect.Intersect(rect, this);

            this.X = result.X;
            this.Y = result.Y;
            this.Width = result.Width;
            this.Height = result.Height;
        }

        public static GRect Intersect(GRect a, GRect b)
        {
            long x1 = Math.Max(a.X, b.X);
            long x2 = Math.Min(a.X + a.Width, b.X + b.Width);
            long y1 = Math.Max(a.Y, b.Y);
            long y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

            if (x2 >= x1
                   && y2 >= y1)
            {

                return new GRect(x1, y1, x2 - x1, y2 - y1);
            }
            return GRect.Empty;
        }

        public bool IntersectsWith(GRect rect)
        {
            return (rect.X < this.X + this.Width) &&
               (this.X < (rect.X + rect.Width)) &&
               (rect.Y < this.Y + this.Height) &&
               (this.Y < rect.Y + rect.Height);
        }

        public static GRect Union(GRect a, GRect b)
        {
            long x1 = Math.Min(a.X, b.X);
            long x2 = Math.Max(a.X + a.Width, b.X + b.Width);
            long y1 = Math.Min(a.Y, b.Y);
            long y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);

            return new GRect(x1, y1, x2 - x1, y2 - y1);
        }

        public void Offset(GPoint pos)
        {
            Offset(pos.X, pos.Y);
        }

        public void OffsetNegative(GPoint pos)
        {
            Offset(-pos.X, -pos.Y);
        }

        public void Offset(long x, long y)
        {
            this.X += x;
            this.Y += y;
        }

        public override string ToString()
        {
            return "{X=" + X.ToString(CultureInfo.CurrentCulture) + ",Y=" + Y.ToString(CultureInfo.CurrentCulture) +
               ",Width=" + Width.ToString(CultureInfo.CurrentCulture) +
               ",Height=" + Height.ToString(CultureInfo.CurrentCulture) + "}";
        }
    }
    public struct GSize
    {
        public static readonly GSize Empty = new GSize();

        private long width;
        private long height;

        public GSize(GPoint pt)
        {
            width = pt.X;
            height = pt.Y;
        }

        public GSize(long width, long height)
        {
            this.width = width;
            this.height = height;
        }

        public static GSize operator +(GSize sz1, GSize sz2)
        {
            return Add(sz1, sz2);
        }

        public static GSize operator -(GSize sz1, GSize sz2)
        {
            return Subtract(sz1, sz2);
        }

        public static bool operator ==(GSize sz1, GSize sz2)
        {
            return sz1.Width == sz2.Width && sz1.Height == sz2.Height;
        }

        public static bool operator !=(GSize sz1, GSize sz2)
        {
            return !(sz1 == sz2);
        }

        public static explicit operator GPoint(GSize size)
        {
            return new GPoint(size.Width, size.Height);
        }

        public bool IsEmpty
        {
            get
            {
                return width == 0 && height == 0;
            }
        }

        public long Width
        {
            get
            {
                return width;
            }
            set
            {
                width = value;
            }
        }

        public long Height
        {
            get
            {
                return height;
            }
            set
            {
                height = value;
            }
        }

        public static GSize Add(GSize sz1, GSize sz2)
        {
            return new GSize(sz1.Width + sz2.Width, sz1.Height + sz2.Height);
        }

        public static GSize Subtract(GSize sz1, GSize sz2)
        {
            return new GSize(sz1.Width - sz2.Width, sz1.Height - sz2.Height);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GSize))
                return false;

            GSize comp = (GSize)obj;
            // Note value types can't have derived classes, so we don't need to
            //
            return (comp.width == this.width) &&
                      (comp.height == this.height);
        }

        public override int GetHashCode()
        {
            if (this.IsEmpty)
            {
                return 0;
            }
            return (Width.GetHashCode() ^ Height.GetHashCode());
        }

        public override string ToString()
        {
            return "{Width=" + width.ToString(CultureInfo.CurrentCulture) + ", Height=" + height.ToString(CultureInfo.CurrentCulture) + "}";
        }
    }
    public struct Placemark
    {
        string address;

        /// <summary>
        /// the address
        /// </summary>
        public string Address
        {
            get
            {
                return address;
            }
            internal set
            {
                address = value;
            }
        }

        /// <summary>
        /// the accuracy of address
        /// </summary>
        public int Accuracy;

        // parsed values from address      
        public string ThoroughfareName;
        public string LocalityName;
        public string PostalCodeNumber;
        public string CountryName;
        public string AdministrativeAreaName;
        public string DistrictName;
        public string SubAdministrativeAreaName;
        public string Neighborhood;
        public string StreetNumber;

        public string CountryNameCode;
        public string HouseNo;

        internal Placemark(string address)
        {
            this.address = address;

            Accuracy = 0;
            HouseNo = string.Empty;
            ThoroughfareName = string.Empty;
            DistrictName = string.Empty;
            LocalityName = string.Empty;
            PostalCodeNumber = string.Empty;
            CountryName = string.Empty;
            CountryNameCode = string.Empty;
            AdministrativeAreaName = string.Empty;
            SubAdministrativeAreaName = string.Empty;
            Neighborhood = string.Empty;
            StreetNumber = string.Empty;
        }
    }
    public struct PointLatLng
    {
        public static readonly PointLatLng Empty = new PointLatLng();
        private double lat;
        private double lng;

        bool NotEmpty;

        public PointLatLng(double lat, double lng)
        {
            this.lat = lat;
            this.lng = lng;
            NotEmpty = true;
        }

        /// <summary>
        /// returns true if coordinates wasn't assigned
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return !NotEmpty;
            }
        }

        public double Lat
        {
            get
            {
                return this.lat;
            }
            set
            {
                this.lat = value;
                NotEmpty = true;
            }
        }

        public double Lng
        {
            get
            {
                return this.lng;
            }
            set
            {
                this.lng = value;
                NotEmpty = true;
            }
        }

        public static PointLatLng operator +(PointLatLng pt, SizeLatLng sz)
        {
            return Add(pt, sz);
        }

        public static PointLatLng operator -(PointLatLng pt, SizeLatLng sz)
        {
            return Subtract(pt, sz);
        }

        public static SizeLatLng operator -(PointLatLng pt1, PointLatLng pt2)
        {
            return new SizeLatLng(pt1.Lat - pt2.Lat, pt2.Lng - pt1.Lng);
        }

        public static bool operator ==(PointLatLng left, PointLatLng right)
        {
            return ((left.Lng == right.Lng) && (left.Lat == right.Lat));
        }

        public static bool operator !=(PointLatLng left, PointLatLng right)
        {
            return !(left == right);
        }

        public static PointLatLng Add(PointLatLng pt, SizeLatLng sz)
        {
            return new PointLatLng(pt.Lat - sz.HeightLat, pt.Lng + sz.WidthLng);
        }

        public static PointLatLng Subtract(PointLatLng pt, SizeLatLng sz)
        {
            return new PointLatLng(pt.Lat + sz.HeightLat, pt.Lng - sz.WidthLng);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PointLatLng))
            {
                return false;
            }
            PointLatLng tf = (PointLatLng)obj;
            return (((tf.Lng == this.Lng) && (tf.Lat == this.Lat)) && tf.GetType().Equals(base.GetType()));
        }

        public void Offset(PointLatLng pos)
        {
            this.Offset(pos.Lat, pos.Lng);
        }

        public void Offset(double lat, double lng)
        {
            this.Lng += lng;
            this.Lat -= lat;
        }

        public override int GetHashCode()
        {
            return (this.Lng.GetHashCode() ^ this.Lat.GetHashCode());
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{{Lat={0}, Lng={1}}}", this.Lat, this.Lng);
        }
    }
    public struct RectLatLng
    {
        public static readonly RectLatLng Empty;
        private double lng;
        private double lat;
        private double widthLng;
        private double heightLat;

        public RectLatLng(double lat, double lng, double widthLng, double heightLat)
        {
            this.lng = lng;
            this.lat = lat;
            this.widthLng = widthLng;
            this.heightLat = heightLat;
            NotEmpty = true;
        }

        public RectLatLng(PointLatLng location, SizeLatLng size)
        {
            this.lng = location.Lng;
            this.lat = location.Lat;
            this.widthLng = size.WidthLng;
            this.heightLat = size.HeightLat;
            NotEmpty = true;
        }

        public static RectLatLng FromLTRB(double leftLng, double topLat, double rightLng, double bottomLat)
        {
            return new RectLatLng(topLat, leftLng, rightLng - leftLng, topLat - bottomLat);
        }

        public PointLatLng LocationTopLeft
        {
            get
            {
                return new PointLatLng(this.Lat, this.Lng);
            }
            set
            {
                this.Lng = value.Lng;
                this.Lat = value.Lat;
            }
        }

        public PointLatLng LocationRightBottom
        {
            get
            {
                PointLatLng ret = new PointLatLng(this.Lat, this.Lng);
                ret.Offset(HeightLat, WidthLng);
                return ret;
            }
        }

        public PointLatLng LocationMiddle
        {
            get
            {
                PointLatLng ret = new PointLatLng(this.Lat, this.Lng);
                ret.Offset(HeightLat / 2, WidthLng / 2);
                return ret;
            }
        }

        public SizeLatLng Size
        {
            get
            {
                return new SizeLatLng(this.HeightLat, this.WidthLng);
            }
            set
            {
                this.WidthLng = value.WidthLng;
                this.HeightLat = value.HeightLat;
            }
        }

        public double Lng
        {
            get
            {
                return this.lng;
            }
            set
            {
                this.lng = value;
            }
        }

        public double Lat
        {
            get
            {
                return this.lat;
            }
            set
            {
                this.lat = value;
            }
        }

        public double WidthLng
        {
            get
            {
                return this.widthLng;
            }
            set
            {
                this.widthLng = value;
            }
        }

        public double HeightLat
        {
            get
            {
                return this.heightLat;
            }
            set
            {
                this.heightLat = value;
            }
        }

        public double Left
        {
            get
            {
                return this.Lng;
            }
        }

        public double Top
        {
            get
            {
                return this.Lat;
            }
        }

        public double Right
        {
            get
            {
                return (this.Lng + this.WidthLng);
            }
        }

        public double Bottom
        {
            get
            {
                return (this.Lat - this.HeightLat);
            }
        }

        bool NotEmpty;

        /// <summary>
        /// returns true if coordinates wasn't assigned
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return !NotEmpty;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RectLatLng))
            {
                return false;
            }
            RectLatLng ef = (RectLatLng)obj;
            return ((((ef.Lng == this.Lng) && (ef.Lat == this.Lat)) && (ef.WidthLng == this.WidthLng)) && (ef.HeightLat == this.HeightLat));
        }

        public static bool operator ==(RectLatLng left, RectLatLng right)
        {
            return ((((left.Lng == right.Lng) && (left.Lat == right.Lat)) && (left.WidthLng == right.WidthLng)) && (left.HeightLat == right.HeightLat));
        }

        public static bool operator !=(RectLatLng left, RectLatLng right)
        {
            return !(left == right);
        }

        public bool Contains(double lat, double lng)
        {
            return ((((this.Lng <= lng) && (lng < (this.Lng + this.WidthLng))) && (this.Lat >= lat)) && (lat > (this.Lat - this.HeightLat)));
        }

        public bool Contains(PointLatLng pt)
        {
            return this.Contains(pt.Lat, pt.Lng);
        }

        public bool Contains(RectLatLng rect)
        {
            return ((((this.Lng <= rect.Lng) && ((rect.Lng + rect.WidthLng) <= (this.Lng + this.WidthLng))) && (this.Lat >= rect.Lat)) && ((rect.Lat - rect.HeightLat) >= (this.Lat - this.HeightLat)));
        }

        public override int GetHashCode()
        {
            if (this.IsEmpty)
            {
                return 0;
            }
            return (((this.Lng.GetHashCode() ^ this.Lat.GetHashCode()) ^ this.WidthLng.GetHashCode()) ^ this.HeightLat.GetHashCode());
        }

        // from here down need to test each function to be sure they work good
        // |
        // .

        #region -- unsure --
        public void Inflate(double lat, double lng)
        {
            this.Lng -= lng;
            this.Lat += lat;
            this.WidthLng += 2d * lng;
            this.HeightLat += 2d * lat;
        }

        public void Inflate(SizeLatLng size)
        {
            this.Inflate(size.HeightLat, size.WidthLng);
        }

        public static RectLatLng Inflate(RectLatLng rect, double lat, double lng)
        {
            RectLatLng ef = rect;
            ef.Inflate(lat, lng);
            return ef;
        }

        public void Intersect(RectLatLng rect)
        {
            RectLatLng ef = Intersect(rect, this);
            this.Lng = ef.Lng;
            this.Lat = ef.Lat;
            this.WidthLng = ef.WidthLng;
            this.HeightLat = ef.HeightLat;
        }

        // ok ???
        public static RectLatLng Intersect(RectLatLng a, RectLatLng b)
        {
            double lng = Math.Max(a.Lng, b.Lng);
            double num2 = Math.Min((double)(a.Lng + a.WidthLng), (double)(b.Lng + b.WidthLng));

            double lat = Math.Max(a.Lat, b.Lat);
            double num4 = Math.Min((double)(a.Lat + a.HeightLat), (double)(b.Lat + b.HeightLat));

            if ((num2 >= lng) && (num4 >= lat))
            {
                return new RectLatLng(lat, lng, num2 - lng, num4 - lat);
            }
            return Empty;
        }

        // ok ???
        // http://greatmaps.codeplex.com/workitem/15981
        public bool IntersectsWith(RectLatLng a)
        {
            return this.Left < a.Right && this.Top > a.Bottom && this.Right > a.Left && this.Bottom < a.Top;
        }

        // ok ???
        // http://greatmaps.codeplex.com/workitem/15981
        public static RectLatLng Union(RectLatLng a, RectLatLng b)
        {
            return RectLatLng.FromLTRB(
               Math.Min(a.Left, b.Left),
               Math.Max(a.Top, b.Top),
               Math.Max(a.Right, b.Right),
               Math.Min(a.Bottom, b.Bottom));
        }
        #endregion

        // .
        // |
        // unsure ends here

        public void Offset(PointLatLng pos)
        {
            this.Offset(pos.Lat, pos.Lng);
        }

        public void Offset(double lat, double lng)
        {
            this.Lng += lng;
            this.Lat -= lat;
        }

        public override string ToString()
        {
            return ("{Lat=" + this.Lat.ToString(CultureInfo.CurrentCulture) + ",Lng="
                   + this.Lng.ToString(CultureInfo.CurrentCulture) + ",WidthLng="
                   + this.WidthLng.ToString(CultureInfo.CurrentCulture)
                   + ",HeightLat=" + this.HeightLat.ToString(CultureInfo.CurrentCulture) + "}");
        }

        static RectLatLng()
        {
            Empty = new RectLatLng();
        }
    }
    public struct SizeLatLng
    {
        public static readonly SizeLatLng Empty;

        private double heightLat;
        private double widthLng;

        public SizeLatLng(SizeLatLng size)
        {
            this.widthLng = size.widthLng;
            this.heightLat = size.heightLat;
        }

        public SizeLatLng(PointLatLng pt)
        {
            this.heightLat = pt.Lat;
            this.widthLng = pt.Lng;
        }

        public SizeLatLng(double heightLat, double widthLng)
        {
            this.heightLat = heightLat;
            this.widthLng = widthLng;
        }

        public static SizeLatLng operator +(SizeLatLng sz1, SizeLatLng sz2)
        {
            return Add(sz1, sz2);
        }

        public static SizeLatLng operator -(SizeLatLng sz1, SizeLatLng sz2)
        {
            return Subtract(sz1, sz2);
        }

        public static bool operator ==(SizeLatLng sz1, SizeLatLng sz2)
        {
            return ((sz1.WidthLng == sz2.WidthLng) && (sz1.HeightLat == sz2.HeightLat));
        }

        public static bool operator !=(SizeLatLng sz1, SizeLatLng sz2)
        {
            return !(sz1 == sz2);
        }

        public static explicit operator PointLatLng(SizeLatLng size)
        {
            return new PointLatLng(size.HeightLat, size.WidthLng);
        }

        public bool IsEmpty
        {
            get
            {
                return ((this.widthLng == 0d) && (this.heightLat == 0d));
            }
        }

        public double WidthLng
        {
            get
            {
                return this.widthLng;
            }
            set
            {
                this.widthLng = value;
            }
        }

        public double HeightLat
        {
            get
            {
                return this.heightLat;
            }
            set
            {
                this.heightLat = value;
            }
        }

        public static SizeLatLng Add(SizeLatLng sz1, SizeLatLng sz2)
        {
            return new SizeLatLng(sz1.HeightLat + sz2.HeightLat, sz1.WidthLng + sz2.WidthLng);
        }

        public static SizeLatLng Subtract(SizeLatLng sz1, SizeLatLng sz2)
        {
            return new SizeLatLng(sz1.HeightLat - sz2.HeightLat, sz1.WidthLng - sz2.WidthLng);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SizeLatLng))
            {
                return false;
            }
            SizeLatLng ef = (SizeLatLng)obj;
            return (((ef.WidthLng == this.WidthLng) && (ef.HeightLat == this.HeightLat)) && ef.GetType().Equals(base.GetType()));
        }

        public override int GetHashCode()
        {
            if (this.IsEmpty)
            {
                return 0;
            }
            return (this.WidthLng.GetHashCode() ^ this.HeightLat.GetHashCode());
        }

        public PointLatLng ToPointLatLng()
        {
            return (PointLatLng)this;
        }

        public override string ToString()
        {
            return ("{WidthLng=" + this.widthLng.ToString(CultureInfo.CurrentCulture) + ", HeightLng=" + this.heightLat.ToString(CultureInfo.CurrentCulture) + "}");
        }

        static SizeLatLng()
        {
            Empty = new SizeLatLng();
        }
    }
    public struct Tile : IDisposable
    {
        public static readonly Tile Empty = new Tile();

        GPoint pos;
        int zoom;
        PureImage[] overlays;
        long OverlaysCount;

        public readonly bool NotEmpty;

        public Tile(int zoom, GPoint pos)
        {
            this.NotEmpty = true;
            this.zoom = zoom;
            this.pos = pos;
            this.overlays = null;
            this.OverlaysCount = 0;
        }

        public IEnumerable<PureImage> Overlays
        {
            get
            {
#if PocketPC
                for (long i = 0, size = OverlaysCount; i < size; i++)
#else
                for (long i = 0, size = Interlocked.Read(ref OverlaysCount); i < size; i++)
#endif
                {
                    yield return overlays[i];
                }
            }
        }

        internal void AddOverlay(PureImage i)
        {
            if (overlays == null)
            {
                overlays = new PureImage[4];
            }
#if !PocketPC
            overlays[Interlocked.Increment(ref OverlaysCount) - 1] = i;
#else
            overlays[++OverlaysCount - 1] = i;
#endif
        }

        internal bool HasAnyOverlays
        {
            get
            {
#if PocketPC
                return OverlaysCount > 0;
#else
                return Interlocked.Read(ref OverlaysCount) > 0;
#endif
            }
        }

        public int Zoom
        {
            get
            {
                return zoom;
            }
            private set
            {
                zoom = value;
            }
        }

        public GPoint Pos
        {
            get
            {
                return pos;
            }
            private set
            {
                pos = value;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (overlays != null)
            {
#if PocketPC
                for (long i = OverlaysCount - 1; i >= 0; i--)

#else
                for (long i = Interlocked.Read(ref OverlaysCount) - 1; i >= 0; i--)
#endif
                {
#if !PocketPC
                    Interlocked.Decrement(ref OverlaysCount);
#else
                    OverlaysCount--;
#endif
                    overlays[i].Dispose();
                    overlays[i] = null;
                }
                overlays = null;
            }
        }

        #endregion

        public static bool operator ==(Tile m1, Tile m2)
        {
            return m1.pos == m2.pos && m1.zoom == m2.zoom;
        }

        public static bool operator !=(Tile m1, Tile m2)
        {
            return !(m1 == m2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Tile))
                return false;

            Tile comp = (Tile)obj;
            return comp.Zoom == this.Zoom && comp.Pos == this.Pos;
        }

        public override int GetHashCode()
        {
            return zoom ^ pos.GetHashCode();
        }
    }
    public struct GPoint
    {
        public static readonly GPoint Empty = new GPoint();

        private long x;
        private long y;

        public GPoint(long x, long y)
        {
            this.x = x;
            this.y = y;
        }

        public GPoint(GSize sz)
        {
            this.x = sz.Width;
            this.y = sz.Height;
        }

        //public GPoint(int dw)
        //{
        //   this.x = (short) LOWORD(dw);
        //   this.y = (short) HIWORD(dw);
        //}

        public bool IsEmpty
        {
            get
            {
                return x == 0 && y == 0;
            }
        }

        public long X
        {
            get
            {
                return x;
            }
            set
            {
                x = value;
            }
        }

        public long Y
        {
            get
            {
                return y;
            }
            set
            {
                y = value;
            }
        }

        public static explicit operator GSize(GPoint p)
        {
            return new GSize(p.X, p.Y);
        }

        public static GPoint operator +(GPoint pt, GSize sz)
        {
            return Add(pt, sz);
        }

        public static GPoint operator -(GPoint pt, GSize sz)
        {
            return Subtract(pt, sz);
        }

        public static bool operator ==(GPoint left, GPoint right)
        {
            return left.X == right.X && left.Y == right.Y;
        }

        public static bool operator !=(GPoint left, GPoint right)
        {
            return !(left == right);
        }

        public static GPoint Add(GPoint pt, GSize sz)
        {
            return new GPoint(pt.X + sz.Width, pt.Y + sz.Height);
        }

        public static GPoint Subtract(GPoint pt, GSize sz)
        {
            return new GPoint(pt.X - sz.Width, pt.Y - sz.Height);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GPoint))
                return false;
            GPoint comp = (GPoint)obj;
            return comp.X == this.X && comp.Y == this.Y;
        }

        public override int GetHashCode()
        {
            return (int)(x ^ y);
        }

        public void Offset(long dx, long dy)
        {
            X += dx;
            Y += dy;
        }

        public void Offset(GPoint p)
        {
            Offset(p.X, p.Y);
        }
        public void OffsetNegative(GPoint p)
        {
            Offset(-p.X, -p.Y);
        }

        public override string ToString()
        {
            return "{X=" + X.ToString(CultureInfo.CurrentCulture) + ",Y=" + Y.ToString(CultureInfo.CurrentCulture) + "}";
        }

        //private static int HIWORD(int n)
        //{
        //   return (n >> 16) & 0xffff;
        //}

        //private static int LOWORD(int n)
        //{
        //   return n & 0xffff;
        //}
    }
    internal struct LoadTask : IEquatable<LoadTask>
    {
        public GPoint Pos;
        public int Zoom;

        internal Core Core;

        public LoadTask(GPoint pos, int zoom, Core core = null)
        {
            Pos = pos;
            Zoom = zoom;
            Core = core;
        }

        public override string ToString()
        {
            return Zoom + " - " + Pos.ToString();
        }

        #region IEquatable<LoadTask> Members

        public bool Equals(LoadTask other)
        {
            return (Zoom == other.Zoom && Pos == other.Pos);
        }

        #endregion
    }
    internal struct RawTile
    {
        public int Type;
        public GPoint Pos;
        public int Zoom;

        public RawTile(int Type, GPoint Pos, int Zoom)
        {
            this.Type = Type;
            this.Pos = Pos;
            this.Zoom = Zoom;
        }

        public override string ToString()
        {
            return Type + " at zoom " + Zoom + ", pos: " + Pos;
        }
    }


    #endregion

    #region Interfaces
    public interface IMInterface
    {
        string CacheLocation
        {
            get;
            set;
        }
        PointLatLng Position
        {
            get;
            set;
        }

        GPoint PositionPixel
        {
            get;
        }
        GMapProvider MapProvider
        {
            get;
            set;
        }
        event TileLoadComplete OnTileLoadComplete;
        event TileLoadStart OnTileLoadStart;
        PointLatLng FromLocalToLatLng(int x, int y);
        GPoint FromLatLngToLocal(PointLatLng point);
    }
    public interface IPureImageCache
    {
        /// <summary>
        /// puts image to db
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="type"></param>
        /// <param name="pos"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        bool PutImageToCache(byte[] tile, int type, GPoint pos, int zoom);

        /// <summary>
        /// gets image from db
        /// </summary>
        /// <param name="type"></param>
        /// <param name="pos"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        PureImage GetImageFromCache(int type, GPoint pos, int zoom);

        /// <summary>
        /// delete old tiles beyond a supplied date
        /// </summary>
        /// <param name="date">Tiles older than this will be deleted.</param>
        /// <param name="type">provider dbid or null to use all providers</param>
        /// <returns>The number of deleted tiles.</returns>
        int DeleteOlderThan(DateTime date, int? type);
    }
    #endregion

    #region delegates
    public delegate void PositionChanged(PointLatLng point);

    public delegate void TileLoadComplete(long ElapsedMilliseconds);
    public delegate void TileLoadStart();

    public delegate void TileCacheComplete();
    public delegate void TileCacheStart();
    public delegate void TileCacheProgress(int tilesLeft);

    public delegate void MapDrag();
    public delegate void MapZoomChanged();
    public delegate void MapTypeChanged(GMapProvider type);

    public delegate void EmptyTileError(int zoom, GPoint pos);
    #endregion

    #region classes
    internal class GPointComparer : IEqualityComparer<GPoint>
    {
        public bool Equals(GPoint x, GPoint y)
        {
            return x.X == y.X && x.Y == y.Y;
        }

        public int GetHashCode(GPoint obj)
        {
            return obj.GetHashCode();
        }
    }
    internal class LoadTaskComparer : IEqualityComparer<LoadTask>
    {
        public bool Equals(LoadTask x, LoadTask y)
        {
            return x.Zoom == y.Zoom && x.Pos == y.Pos;
        }

        public int GetHashCode(LoadTask obj)
        {
            return obj.Zoom ^ obj.Pos.GetHashCode();
        }
    }
    internal class RawTileComparer : IEqualityComparer<RawTile>
    {
        public bool Equals(RawTile x, RawTile y)
        {
            return x.Type == y.Type && x.Zoom == y.Zoom && x.Pos == y.Pos;
        }

        public int GetHashCode(RawTile obj)
        {
            return obj.Type ^ obj.Zoom ^ obj.Pos.GetHashCode();
        }
    }

    #endregion
}
