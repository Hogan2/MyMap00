using MiniGMap.Core;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MiniGMap.WindowsPresentation
{
    public class GMapImage : PureImage
    {
        public ImageSource Img;

        public override void Dispose()
        {
            if (Img != null)
            {
                Img = null;
            }

            if (Data != null)
            {
                Data.Dispose();
                Data = null;
            }
        }
    }

    /// <summary>
    /// image abstraction proxy
    /// </summary>
    public class GMapImageProxy : PureImageProxy
    {
        GMapImageProxy()
        {

        }

        public static void Enable()
        {
            GMapProvider.TileImageProxy = Instance;
        }

        public static readonly GMapImageProxy Instance = new GMapImageProxy();

        //static readonly byte[] pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        //static readonly byte[] jpgHeader = { 0xFF, 0xD8, 0xFF };
        //static readonly byte[] gifHeader = { 0x47, 0x49, 0x46 };
        //static readonly byte[] bmpHeader = { 0x42, 0x4D };

        public override PureImage FromStream(System.IO.Stream stream)
        {
            GMapImage ret = null;
            if (stream != null)
            {
                var type = stream.ReadByte();
                stream.Position = 0;

                ImageSource m = null;

                switch (type)
                {
                    // PNG: 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A
                    case 0x89:
                        {
                            var bitmapDecoder = new PngBitmapDecoder(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
                            m = bitmapDecoder.Frames[0];
                            bitmapDecoder = null;
                        }
                        break;

                    // JPG: 0xFF, 0xD8, 0xFF
                    case 0xFF:
                        {
                            var bitmapDecoder = new JpegBitmapDecoder(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
                            m = bitmapDecoder.Frames[0];
                            bitmapDecoder = null;
                        }
                        break;

                    // GIF: 0x47, 0x49, 0x46
                    case 0x47:
                        {
                            var bitmapDecoder = new GifBitmapDecoder(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
                            m = bitmapDecoder.Frames[0];
                            bitmapDecoder = null;
                        }
                        break;

                    // BMP: 0x42, 0x4D
                    case 0x42:
                        {
                            var bitmapDecoder = new BmpBitmapDecoder(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
                            m = bitmapDecoder.Frames[0];
                            bitmapDecoder = null;
                        }
                        break;

                    // TIFF: 0x49, 0x49 || 0x4D, 0x4D
                    case 0x49:
                    case 0x4D:
                        {
                            var bitmapDecoder = new TiffBitmapDecoder(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
                            m = bitmapDecoder.Frames[0];
                            bitmapDecoder = null;
                        }
                        break;

                    default:
                        {
                            Debug.WriteLine("WindowsPresentationImageProxy: unknown image format: " + type);
                        }
                        break;
                }

                if (m != null)
                {
                    ret = new GMapImage();
                    ret.Img = m;
                    if (ret.Img.CanFreeze)
                    {
                        ret.Img.Freeze();
                    }
                }
            }
            return ret;
        }

        public override bool Save(System.IO.Stream stream, PureImage image)
        {
            GMapImage ret = (GMapImage)image;
            if (ret.Img != null)
            {
                try
                {
                    PngBitmapEncoder e = new PngBitmapEncoder();
                    e.Frames.Add(BitmapFrame.Create(ret.Img as BitmapSource));
                    e.Save(stream);
                    e = null;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}
