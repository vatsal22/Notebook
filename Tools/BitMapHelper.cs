using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace GettingStarted_Ink.Tools
{
    class BitMapHelper
    {
        // Given a UI element, converts to a RenderTargetBitmap -> regular PNG
        // Sizes the PNG according to the element + screen size
        // Appends the png Image as a child to a Grid for convenience.

        public async Task<Grid> UIElementToGridImage (UIElement element)
        {
            var renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(element);

            var bitmapImage = await ConvertToBitmapImage(renderTargetBitmap);

            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var image = new Image
            {
                Source = bitmapImage,
                Stretch = Windows.UI.Xaml.Media.Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            grid.Children.Add(image);

            return grid;
        }

        private async Task<BitmapImage> ConvertToBitmapImage(RenderTargetBitmap renderTargetBitmap)
        {
            var pixels = await renderTargetBitmap.GetPixelsAsync();

            var stream = pixels.AsStream();

            var outStream = new InMemoryRandomAccessStream();

            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outStream);

            var displayInformation = DisplayInformation.GetForCurrentView();

            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)renderTargetBitmap.PixelWidth, (uint)renderTargetBitmap.PixelHeight, displayInformation.RawDpiX, displayInformation.RawDpiY, pixels.ToArray());

            await encoder.FlushAsync();
            outStream.Seek(0);

            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(outStream);

            return bitmap;
        }

    }
}
