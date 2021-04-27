
using GettingStarted_Ink.Models;
using GettingStarted_Ink.Tools;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
// Begin "Step 2: Use InkCanvas to support basic inking"
////using directives for inking functionality.
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
// End "Step 2: Use InkCanvas to support basic inking"

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace GettingStarted_Ink
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 




    public sealed partial class MainPage : Page
    {
        //    // Begin "Step 5: Support handwriting recognition"
        //    InkAnalyzer analyzerText = new InkAnalyzer();
        //    IReadOnlyList<InkStroke> strokesText = null;
        //    InkAnalysisResult resultText = null;
        //    IReadOnlyList<IInkAnalysisNode> words = null;
        //    // End "Step 5: Support handwriting recognition"

        //    // Begin "Step 6: Recognize shapes"
        //    InkAnalyzer analyzerShape = new InkAnalyzer();
        //    IReadOnlyList<InkStroke> strokesShape = null;
        //    InkAnalysisResult resultShape = null;
        //    // End "Step 6: Recognize shapes"

        int cur_page = 0;
        int last_page = 0;
        // Use cur_page.toString() + page_file_name
        string page_file_name = "_Page.gif";
        public static InkCanvas _inkCanvas;
        public static Grid _renderedGrid;



        private PrintHelper printHelper;
        private BitMapHelper bitMapHelper;

        private List<InkPage> pageBuffers;

        public MainPage()
        {
            this.InitializeComponent();

            printHelper = new PrintHelper();
            bitMapHelper = new BitMapHelper();

            _inkCanvas = inkCanvas;
            _renderedGrid = RenderedGrid;

            // Begin "Step 3: Support inking with touch and mouse"
            inkCanvas.InkPresenter.InputDeviceTypes =
                    Windows.UI.Core.CoreInputDeviceTypes.Mouse |
                    Windows.UI.Core.CoreInputDeviceTypes.Pen;
            // End "Step 3: Support inking with touch and mouse"

            save_ink("blank.gif"); // create a blank file to create new pages later


            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

            IReadOnlyList<StorageFile> files =
                            Task.Run(async () =>
                            {
                                return await storageFolder.GetFilesAsync();
                            }).GetAwaiter().GetResult();


            int num_pages = 0;
            foreach (var file in files)
            {
                // TODO should check that it starts with number and no missing pages (e.g. 0,1,3...)




                if (file.Name.EndsWith(page_file_name))
                {
                    int pageID = int.Parse(file.Name.Split('_')[0]);
                    InkPage ip = new InkPage(pageID);
                    ip.loadAsync().GetAwaiter().GetResult();
                    pageBuffers.Add(ip);

                }
            }

            if (pageBuffers.Count > 0)
            {
                last_page = num_pages - 1;

                // load 0_Page.gif
                load_ink(cur_page.ToString() + page_file_name);
            } // else last_page=0, which is the current blank page

        }

        private void objectManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var stackDragged = e.OriginalSource as StackPanel;
            (stackDragged.RenderTransform as TranslateTransform).X += e.Delta.Translation.X;
            (stackDragged.RenderTransform as TranslateTransform).Y += e.Delta.Translation.Y;
        }



        private void button_next_click(object sender, RoutedEventArgs e)
        {



            if (cur_page == last_page)
            {
                last_page++;
                cur_page++;
                load_ink("blank.gif");
            }
            else
            {
                go_to_page(cur_page + 1);
            } 
        }

        private void button_prev_click(object sender, RoutedEventArgs e)
        {

            if (cur_page != 0)
            {
                go_to_page(cur_page - 1);


            }
        }

        private void go_to_page(int i)
        {
            save_ink(cur_page.ToString() + page_file_name);

            if (cur_page == i) return;

            cur_page = i;
            load_ink(cur_page.ToString() + page_file_name);

        }


        private async void write_buffers()
        {
            List<Task> tasks = new List<Task>(); 
            foreach (var inkPage in pageBuffers)
            {
                tasks.Add(inkPage.saveAsync());
            }
            foreach (Task task in tasks) await task;
        }

        private async void replaceCanvas(Canvas canvas)
        {
                // TODO use buffers to replace actual displayed canvas
        }

        private async void save_ink(string file_name)
        {
            // Get all strokes on the InkCanvas.
            IReadOnlyList<InkStroke> currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();



            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile file = await storageFolder.CreateFileAsync(file_name, Windows.Storage.CreationCollisionOption.ReplaceExisting);

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
                    await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(outputStream);
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


        private async void load_ink(string file_name)
        {
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
                    await inkCanvas.InkPresenter.StrokeContainer.LoadAsync(inputStream);
                }
                stream.Dispose();
            }
            // User selects Cancel and picker returns null.
            else
            {
                // must be empty, 
            }
        }

        private async void switchPage_Click(object sender, RoutedEventArgs e)
        {

            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();

            await renderTargetBitmap.RenderAsync(inkCanvas);

            RenderedImage.Source = renderTargetBitmap;



            //this.Frame.Navigate(typeof(BlankPage1));

        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            printHelper.register();

        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            printHelper.unregister();

        }

        private async void PrintButtonClick(object sender, RoutedEventArgs e)
        {

            printHelper.ResetElements();


            //save_ink(cur_page.ToString() + page_file_name);


            go_to_page(2);

            printHelper.AddElement(await bitMapHelper.UIElementToGridImage(_PageCanvas));




            // load/iterate through each page to capture the
            // pngs we need to print





            printHelper.showDialog();



        }


    }

}

//    // Begin "Step 5: Support handwriting recognition"
//    private async void recognizeText_ClickAsync(object sender, RoutedEventArgs e)
//    {
//        strokesText = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
//        // Ensure an ink stroke is present.
//        if (strokesText.Count > 0)
//        {
//            analyzerText.AddDataForStrokes(strokesText);

//            // Force analyzer to process strokes as handwriting.
//            foreach (var stroke in strokesText)
//            {
//                analyzerText.SetStrokeDataKind(stroke.Id, InkAnalysisStrokeKind.Writing);
//            }

//            // Clear recognition results string.
//            recognitionResult.Text = "";

//            resultText = await analyzerText.AnalyzeAsync();

//            if (resultText.Status == InkAnalysisStatus.Updated)
//            {
//                var text = analyzerText.AnalysisRoot.RecognizedText;
//                words = analyzerText.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkWord);
//                foreach (var word in words)
//                {
//                    InkAnalysisInkWord concreteWord = (InkAnalysisInkWord)word;
//                    foreach (string s in concreteWord.TextAlternates)
//                    {
//                        recognitionResult.Text += s + " ";
//                    }
//                    recognitionResult.Text += " / ";
//                }
//            }
//            analyzerText.ClearDataForAllStrokes();
//        }
//    }
//    // End "Step 5: Support handwriting recognition"

//    // Begin "Step 6: Recognize shapes"
//    private async void recognizeShape_ClickAsync(object sender, RoutedEventArgs e)
//    {
//        strokesShape = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();

//        if (strokesShape.Count > 0)
//        {
//            analyzerShape.AddDataForStrokes(strokesShape);

//            resultShape = await analyzerShape.AnalyzeAsync();

//            if (resultShape.Status == InkAnalysisStatus.Updated)
//            {
//                var drawings = analyzerShape.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing);

//                foreach (var drawing in drawings)
//                {
//                    var shape = (InkAnalysisInkDrawing)drawing;
//                    if (shape.DrawingKind == InkAnalysisDrawingKind.Drawing)
//                    {
//                        // Catch and process unsupported shapes (lines and so on) here.
//                    }
//                    else
//                    {
//                        // Process recognized shapes here.
//                        if (shape.DrawingKind == InkAnalysisDrawingKind.Circle || shape.DrawingKind == InkAnalysisDrawingKind.Ellipse)
//                        {
//                            DrawEllipse(shape);
//                        }
//                        else
//                        {
//                            DrawPolygon(shape);
//                        }
//                        foreach (var strokeId in shape.GetStrokeIds())
//                        {
//                            var stroke = inkCanvas.InkPresenter.StrokeContainer.GetStrokeById(strokeId);
//                            stroke.Selected = true;
//                        }
//                    }
//                    analyzerShape.RemoveDataForStrokes(shape.GetStrokeIds());
//                }
//                inkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
//            }
//        }
//    }

//    private void DrawEllipse(InkAnalysisInkDrawing shape)
//    {
//        var points = shape.Points;
//        Ellipse ellipse = new Ellipse();
//        ellipse.Width = Math.Sqrt((points[0].X - points[2].X) * (points[0].X - points[2].X) +
//             (points[0].Y - points[2].Y) * (points[0].Y - points[2].Y));
//        ellipse.Height = Math.Sqrt((points[1].X - points[3].X) * (points[1].X - points[3].X) +
//             (points[1].Y - points[3].Y) * (points[1].Y - points[3].Y));

//        var rotAngle = Math.Atan2(points[2].Y - points[0].Y, points[2].X - points[0].X);
//        RotateTransform rotateTransform = new RotateTransform();
//        rotateTransform.Angle = rotAngle * 180 / Math.PI;
//        rotateTransform.CenterX = ellipse.Width / 2.0;
//        rotateTransform.CenterY = ellipse.Height / 2.0;

//        TranslateTransform translateTransform = new TranslateTransform();
//        translateTransform.X = shape.Center.X - ellipse.Width / 2.0;
//        translateTransform.Y = shape.Center.Y - ellipse.Height / 2.0;

//        TransformGroup transformGroup = new TransformGroup();
//        transformGroup.Children.Add(rotateTransform);
//        transformGroup.Children.Add(translateTransform);
//        ellipse.RenderTransform = transformGroup;

//        var brush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(255, 0, 0, 255));
//        ellipse.Stroke = brush;
//        ellipse.StrokeThickness = 2;
//        canvas.Children.Add(ellipse);
//    }

//    private void DrawPolygon(InkAnalysisInkDrawing shape)
//    {
//        var points = shape.Points;
//        Polygon polygon = new Polygon();

//        foreach (var point in points)
//        {
//            polygon.Points.Add(point);
//        }

//        var brush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(255, 0, 0, 255));
//        polygon.Stroke = brush;
//        polygon.StrokeThickness = 2;
//        canvas.Children.Add(polygon);
//    }
//    // End "Step 6: Recognize shapes"

//    // Begin "Step 7: Save and load ink"
//    private async void buttonSave_ClickAsync(object sender, RoutedEventArgs e)
//    {
//        // Get all strokes on the InkCanvas.
//        IReadOnlyList<InkStroke> currentStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();

//        if (currentStrokes.Count > 0)
//        {
//            // Use a file picker to identify ink file.
//            Windows.Storage.Pickers.FileSavePicker savePicker =
//                new Windows.Storage.Pickers.FileSavePicker();
//            savePicker.SuggestedStartLocation =
//                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
//            savePicker.FileTypeChoices.Add(
//                "GIF with embedded ISF",
//                new List<string>() { ".gif" });
//            savePicker.DefaultFileExtension = ".gif";
//            savePicker.SuggestedFileName = "InkSample";

//            // Show the file picker.
//            Windows.Storage.StorageFile file =
//                await savePicker.PickSaveFileAsync();
//            // When selected, picker returns a reference to the file.
//            if (file != null)
//            {
//                // Prevent updates to the file until updates are 
//                // finalized with call to CompleteUpdatesAsync.
//                Windows.Storage.CachedFileManager.DeferUpdates(file);
//                // Open a file stream for writing.
//                IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
//                // Write the ink strokes to the output stream.
//                using (IOutputStream outputStream = stream.GetOutputStreamAt(0))
//                {
//                    await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(outputStream);
//                    await outputStream.FlushAsync();
//                }
//                stream.Dispose();

//                // Finalize write so other apps can update file.
//                Windows.Storage.Provider.FileUpdateStatus status =
//                    await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);

//                if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
//                {
//                    // File saved.
//                }
//                else
//                {
//                    // File couldn't be saved.
//                }
//            }
//            // User selects Cancel and picker returns null.
//            else
//            {
//                // Operation cancelled.
//            }
//        }
//    }

//    private async void buttonLoad_ClickAsync(object sender, RoutedEventArgs e)
//    {
//        // Use a file picker to identify ink file.
//        Windows.Storage.Pickers.FileOpenPicker openPicker =
//            new Windows.Storage.Pickers.FileOpenPicker();
//        openPicker.SuggestedStartLocation =
//            Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
//        openPicker.FileTypeFilter.Add(".gif");
//        // Show the file picker.
//        Windows.Storage.StorageFile file = await openPicker.PickSingleFileAsync();
//        // When selected, picker returns a reference to the file.
//        if (file != null)
//        {
//            // Open a file stream for reading.
//            IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
//            // Read from file.
//            using (var inputStream = stream.GetInputStreamAt(0))
//            {
//                await inkCanvas.InkPresenter.StrokeContainer.LoadAsync(inputStream);
//            }
//            stream.Dispose();
//        }
//        // User selects Cancel and picker returns null.
//        else
//        {
//            // Operation cancelled.
//        }
//    }
//    // End "Step 7: Save and load ink"
//}
