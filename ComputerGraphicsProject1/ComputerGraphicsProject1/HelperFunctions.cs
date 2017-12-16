using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ComputerGraphicsProject1
{
    public class HelperFunctions
    {

        public static WriteableBitmap resize_image(WriteableBitmap img, double scale)
        {
            BitmapSource source = img;

            var s = new ScaleTransform(scale, scale);

            var res = new TransformedBitmap(img, s);

            return convert_BitmapSource_to_WriteableBitmap(res);
        }

        private static WriteableBitmap convert_BitmapSource_to_WriteableBitmap(BitmapSource source)
        {
            // Calculate stride of source
            int stride = source.PixelWidth * (source.Format.BitsPerPixel / 8);

            // Create data array to hold source pixel data
            byte[] data = new byte[stride * source.PixelHeight];

            // Copy source image pixels to the data array
            source.CopyPixels(data, stride, 0);

            // Create WriteableBitmap to copy the pixel data to.      
            WriteableBitmap target = new WriteableBitmap(source.PixelWidth
                , source.PixelHeight, source.DpiX, source.DpiY
                , source.Format, null);

            // Write the pixel data to the WriteableBitmap.
            target.WritePixels(new Int32Rect(0, 0
                , source.PixelWidth, source.PixelHeight)
                , data, stride, 0);

            return target;
        }
    }
}
