using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniGMap.Core
{
    public class AMapSateliteProvider : AMapProviderBase
    {
        public static readonly AMapSateliteProvider Instance;

        private readonly Guid amapID = new Guid("FCA94AF4-3467-47c6-BDA2-6F52E4A145BC");

        private readonly string amapName = "高德卫星地图";

        private readonly string mapName = "AMapSatelite";

        private static readonly string amapurl;

        public override Guid Id
        {
            get
            {
                return this.amapID;
            }
        }

        public string CnName
        {
            get
            {
                return this.amapName;
            }
        }

        public override string Name
        {
            get
            {
                return this.mapName;
            }
        }

        static AMapSateliteProvider()
        {
            AMapSateliteProvider.amapurl = "http://webst0{0}.is.autonavi.com/appmaptile?style=6&x={1}&y={2}&z={3}";
            AMapSateliteProvider.Instance = new AMapSateliteProvider();
            //GMapProviders.AddMapProvider(AMapSateliteProvider.Instance);
        }

        public override PureImage GetTileImage(GPoint pos, int zoom)
        {
            PureImage result;
            try
            {
                if (true)
                {
                }
                string url = this.a(pos, zoom, GMapProvider.LanguageStr);
                result = base.GetTileImageUsingHttp(url);
            }
            catch (Exception)
            {
                result = null;
            }
            return result;
        }

        private string a(GPoint A_0, int A_1, string A_2)
        {
            if (true)
            {
            }
            long num = (A_0.X + A_0.Y) % 4L + 1L;
            return string.Format(AMapSateliteProvider.amapurl, new object[]
            {
                num,
                A_0.X,
                A_0.Y,
                A_1
            });
        }
    }
}
