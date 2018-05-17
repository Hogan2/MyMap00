namespace MiniGMap.Core
{
    public abstract class AMapProviderBase : GMapProvider
    {
        private GMapProvider[] a;

        public override PureProjection Projection
        {
            get
            {
                return MercatorProjection.Instance;
            }
        }

        public override GMapProvider[] Overlays
        {
            get
            {
                int num = 1;
                while (true)
                {
                    switch (num)
                    {
                        case 0:
                            this.a = new GMapProvider[]
                            {
                            this
                            };
                            num = 2;
                            continue;
                        case 2:
                            goto IL_4E;
                    }
                    if (true)
                    {
                    }
                    if (this.a != null)
                    {
                        break;
                    }
                    num = 0;
                }
                IL_4E:
                return this.a;
            }
        }

        public AMapProviderBase()
        {
            this.MaxZoom = new int?(18);
            this.MinZoom = 3;
            this.RefererUrl = "http://www.amap.com/";
        }
    }
}
