using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MiniGMap.Core
{
    public abstract class GoogleMapProviderBase : GMapProvider
    {
        public GoogleMapProviderBase()
        {
            MaxZoom = null;
            RefererUrl = string.Format("http://maps.{0}/", Server);
            Copyright = null;// string.Format("©{0} Google - Map data ©{0} Tele Atlas, Imagery ©{0} TerraMetrics", DateTime.Today.Year);
        }

        public readonly string ServerAPIs /* ;}~~ */ = Stuff.GString(/*{^_^}*/"9gERyvblybF8iMuCt/LD6w=="/*d{'_'}b*/);
        public readonly string Server /* ;}~~~~ */ = Stuff.GString(/*{^_^}*/"gosr2U13BoS+bXaIxt6XWg=="/*d{'_'}b*/);
        public readonly string ServerChina /* ;}~ */ = Stuff.GString(/*{^_^}*/"gosr2U13BoTEJoJJuO25gQ=="/*d{'_'}b*/);
        public readonly string ServerKorea /* ;}~~ */ = Stuff.GString(/*{^_^}*/"8ZVBOEsBinzi+zmP7y7pPA=="/*d{'_'}b*/);
        public readonly string ServerKoreaKr /* ;}~ */ = Stuff.GString(/*{^_^}*/"gosr2U13BoQyz1gkC4QLfg=="/*d{'_'}b*/);

        public string SecureWord = "Galileo";

        /// <summary>
        /// Your application's API key, obtained from the Google Developers Console.
        /// This key identifies your application for purposes of quota management. 
        /// Must provide either API key or Maps for Work credentials.
        /// </summary>
        public string ApiKey = string.Empty;

        #region GMapProvider Members
        public override Guid Id
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string Name
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override PureProjection Projection
        {
            get
            {
                return MercatorProjection.Instance;
            }
        }

        GMapProvider[] overlays;
        public override GMapProvider[] Overlays
        {
            get
            {
                if (overlays == null)
                {
                    overlays = new GMapProvider[] { this };
                }
                return overlays;
            }
        }

        public override PureImage GetTileImage(GPoint pos, int zoom)
        {
            throw new NotImplementedException();
        }
        #endregion

        public bool TryCorrectVersion = true;
        static bool init = false;

        public override void OnInitialized()
        {
            if (!init && TryCorrectVersion)
            {
                string url = string.Format("https://maps.{0}/maps/api/js?client=google-maps-lite&amp;libraries=search&amp;language=en&amp;region=", ServerAPIs);
                try
                {
                    string html = GMaps.Instance.UseUrlCache ? Cache.Instance.GetContent(url, CacheType.UrlCache, TimeSpan.FromHours(8)) : string.Empty;

                    if (string.IsNullOrEmpty(html))
                    {
                        html = null;//GetContentUsingHttp(url);
                        if (!string.IsNullOrEmpty(html))
                        {
                            if (GMaps.Instance.UseUrlCache)
                            {
                                Cache.Instance.SaveContent(url, CacheType.UrlCache, html);
                            }
                        }
                    }


                    init = true; // try it only once
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("TryCorrectGoogleVersions failed: " + ex.ToString());
                }
            }
        }

        internal void GetSecureWords(GPoint pos, out string sec1, out string sec2)
        {
            sec1 = string.Empty; // after &x=...
            sec2 = string.Empty; // after &zoom=...
            int seclen = (int)((pos.X * 3) + pos.Y) % 8;
            sec2 = SecureWord.Substring(0, seclen);
            if (pos.Y >= 10000 && pos.Y < 100000)
            {
                sec1 = Sec1;
            }
        }

        static readonly string Sec1 = "&s=";


        #region -- Maps API for Work --
        /// <summary>
        /// https://developers.google.com/maps/documentation/business/webservices/auth#how_do_i_get_my_signing_key
        /// To access the special features of the Google Maps API for Work you must provide a client ID
        /// when accessing any of the API libraries or services.
        /// When registering for Google Google Maps API for Work you will receive this client ID from Enterprise Support.
        /// All client IDs begin with a gme- prefix. Your client ID is passed as the value of the client parameter.
        /// Generally, you should store your private key someplace safe and read them into your code
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="privateKey"></param>
        public void SetEnterpriseCredentials(string clientId, string privateKey)
        {
            privateKey = privateKey.Replace("-", "+").Replace("_", "/");
            _privateKeyBytes = Convert.FromBase64String(privateKey);
            _clientId = clientId;
        }
        private byte[] _privateKeyBytes;

        private string _clientId = string.Empty;

        /// <summary>
        /// Your client ID. To access the special features of the Google Maps API for Work
        /// you must provide a client ID when accessing any of the API libraries or services.
        /// When registering for Google Google Maps API for Work you will receive this client ID
        /// from Enterprise Support. All client IDs begin with a gme- prefix.
        /// </summary>
        public string ClientId
        {
            get
            {
                return _clientId;
            }
        }

        string GetSignedUri(Uri uri)
        {
            var builder = new UriBuilder(uri);
            builder.Query = builder.Query.Substring(1) + "&client=" + _clientId;
            uri = builder.Uri;
            string signature = GetSignature(uri);

            return uri.Scheme + "://" + uri.Host + uri.LocalPath + uri.Query + "&signature=" + signature;
        }

        string GetSignedUri(string url)
        {
            return GetSignedUri(new Uri(url));
        }

        string GetSignature(Uri uri)
        {
            byte[] encodedPathQuery = Encoding.ASCII.GetBytes(uri.LocalPath + uri.Query);
            var hashAlgorithm = new HMACSHA1(_privateKeyBytes);
            byte[] hashed = hashAlgorithm.ComputeHash(encodedPathQuery);
            return Convert.ToBase64String(hashed).Replace("+", "-").Replace("/", "_");
        }
        #endregion
    }
}
