using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace GettingStarted_Ink.Models
{

    class InkPage
    {

        private static string page_file_name = "_Page.gif";
        private bool deleted = false; 


        public Grid _grid { get; set; }
        
        // 0 indexed 
        // "Normal" Pages: >=0
        // e.g. 0,1,2,3...
        // "Special" pages: <0
        // E.g. blank page is -1
        public int _pageID { get;set; }



        public InkPage(int pageID) 
        {
            _pageID = pageID;
            _grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Width = 620,
                Height = 800,
                BorderBrush=new SolidColorBrush(Colors.Black),
                BorderThickness=new Thickness(1),
                Background= new SolidColorBrush(Colors.Beige)
            };

            var inkCanvas = new InkCanvas();
            inkCanvas.InkPresenter.InputDeviceTypes =
                    Windows.UI.Core.CoreInputDeviceTypes.Mouse |
                    Windows.UI.Core.CoreInputDeviceTypes.Pen;
            _grid.Children.Add(inkCanvas);
        }

        public InkCanvas GetInkCanvas()
        {
            return _grid.Children.OfType<InkCanvas>().First();
        }

        public async Task deleteAsync()
        {


            if (!deleted)
            {
                deleted = true;
            }
            else
            {
                return;
            }

            string file_name = _pageID.ToString() + page_file_name;

            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile file = await storageFolder.CreateFileAsync(file_name, Windows.Storage.CreationCollisionOption.ReplaceExisting);



            await file.DeleteAsync();


            return;


        }


        public async Task loadAsync()
        {
            string file_name = _pageID.ToString() + page_file_name;

            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile file = await storageFolder.GetFileAsync(file_name);


            // When selected, picker returns a reference to the file.
            if (file != null)
            {
                // Open a file stream for reading.
                IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                // Read from file.
                using (var inputStream = stream.GetInputStreamAt(0))
                {
                    await GetInkCanvas().InkPresenter.StrokeContainer.LoadAsync(inputStream);
                }
                stream.Dispose();
            }
            // User selects Cancel and picker returns null.
            else
            {
                // must be empty, 
            }
        }

        public async Task saveAsync()
        {
            string file_name = _pageID.ToString() + page_file_name;

            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile file = await storageFolder.CreateFileAsync(file_name, Windows.Storage.CreationCollisionOption.ReplaceExisting);



            // Get all strokes on the InkCanvas.
            IReadOnlyList<InkStroke> currentStrokes = GetInkCanvas().InkPresenter.StrokeContainer.GetStrokes();





            // When selected, picker returns a reference to the file.
            if (file != null)
            {
                // Prevent updates to the file until updates are 
                // finalized with call to CompleteUpdatesAsync.
                Windows.Storage.CachedFileManager.DeferUpdates(file);
                // Open a file stream for writing.
                IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
                // Write the ink strokes to the output stream.
                using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
                {
                    await GetInkCanvas().InkPresenter.StrokeContainer.SaveAsync(outputStream);
                    await outputStream.FlushAsync();
                }
                stream.Dispose();

                // Finalize write so other apps can update file.
                Windows.Storage.Provider.FileUpdateStatus status =
                    await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);

                if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
                {
                    // File saved.
                }
                else
                {
                    // File couldn't be saved.
                }

            }
        }


    }

    class InkPageComparer : Comparer<InkPage>
    {
        public override int Compare(InkPage x, InkPage y)
        {
            return x._pageID.CompareTo(y._pageID);
        }
    }
}
