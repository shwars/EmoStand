using Microsoft.ProjectOxford.Emotion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Core;
using Windows.Media.FaceAnalysis;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Microsoft.ProjectOxford.Common;

// Документацию по шаблону элемента "Пустая страница" см. по адресу http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace EmoStand
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        static string OxfordAPIKey = "f5b90f6308064c03918579f46b976112"; // "<Your Key Here>";

        MediaCapture MC;
        DispatcherTimer dt = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(3) };
        EmotionServiceClient Oxford = new EmotionServiceClient(OxfordAPIKey);

        EmoCollection MyEmo = new EmoCollection();

        FaceDetectionEffect FaceDetector;
        VideoEncodingProperties VideoProps;

        bool IsFacePresent = false;

        FaceCollection Faces = new FaceCollection();
        string last_emo = "";

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await Init();
            dt.Tick += GetEmotions;
            dt.Start();
        }

        private async Task Init()
        {
            MC = new MediaCapture();
            var cameras = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            var camera = cameras.First();
            var settings = new MediaCaptureInitializationSettings() { VideoDeviceId = camera.Id };
            await MC.InitializeAsync(settings);
            ViewFinder.Source = MC;
            
            // Create face detection
            var def = new FaceDetectionEffectDefinition();
            def.SynchronousDetectionEnabled = false;
            def.DetectionMode = FaceDetectionMode.HighPerformance;
            FaceDetector = (FaceDetectionEffect)(await MC.AddVideoEffectAsync(def, MediaStreamType.VideoPreview));
            FaceDetector.FaceDetected += FaceDetectedEvent;
            FaceDetector.DesiredDetectionInterval = TimeSpan.FromMilliseconds(100);
            FaceDetector.Enabled = true;

            await MC.StartPreviewAsync();
            var props = MC.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            VideoProps = props as VideoEncodingProperties;
        }

        private async void FaceDetectedEvent(FaceDetectionEffect sender, FaceDetectedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => HighlightDetectedFace(args.ResultFrame.DetectedFaces.FirstOrDefault()));
        }

        private async Task HighlightDetectedFace(DetectedFace face)
        {
            var cx = ViewFinder.ActualWidth / VideoProps.Width;
            var cy = ViewFinder.ActualHeight / VideoProps.Height;
                
            if (face==null)
            {
                FaceRect.Visibility = Visibility.Collapsed;
                IsFacePresent = false;
                last_emo = "";
                EmoDesc.Visibility = EmoControl.Visibility = Visibility.Collapsed;
                Desc.Visibility = Visibility.Visible;
            }
            else
            {
                // Canvas.SetLeft(FaceRect, face.FaceBox.X);
                // Canvas.SetTop(FaceRect, face.FaceBox.Y);
                FaceRect.Margin = new Thickness(cx*face.FaceBox.X, cy*face.FaceBox.Y, 0, 0);
                FaceRect.Width = cx*face.FaceBox.Width;
                FaceRect.Height = cy*face.FaceBox.Height;
                FaceRect.Visibility = Visibility.Visible;
                IsFacePresent = true;
                EmoDesc.Visibility = EmoControl.Visibility = Visibility.Visible;
                Desc.Visibility = Visibility.Collapsed;
            }
        }


        async void GetEmotions(object sender, object e)
        {
            // dt.Stop();
            if (!IsFacePresent) return;
            var ms = new MemoryStream();
            await MC.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), ms.AsRandomAccessStream());
            ms.Position = 0L;
            var ms1 = new MemoryStream();
            ms.CopyTo(ms1);
            ms.Position = 0L;
            ms1.Position = 0L;
            var Emo = await Oxford.RecognizeAsync(ms);
            if (Emo != null && Emo.Length > 0)
            {
                var Face = Emo[0];
                var s = Face.Scores;
                MyEmo.Update(Face.Scores);
                var se = MyEmo.StrongestEmotion;
                EmoDesc.Text = $"{se.Emotion} {se.Value500}";
                if (se.Value>0.9 && se.Emotion!=last_emo)
                {
                    last_emo = se.Emotion;
                    Point p; Size sz;
                    ExpandFaceRect(Face.FaceRectangle, out p, out sz);
                    var cb = await CropBitmap.GetCroppedBitmapAsync(ms1.AsRandomAccessStream(), p, sz, 1);
                    Faces.AddFace(se, cb);
                    FacesLine.ItemsSource = null;
                    FacesLine.ItemsSource = Faces.Collection;
                }
            }
        }

        private void ExpandFaceRect(Rectangle r, out Point p, out Size sz)
        {
            var dx = 0.3*(float)r.Width;
            var dy = 0.4 * (float)r.Height;
            var x = (float)r.Left - dx;
            var y = (float)r.Top - dy;
            var h = (float)r.Height + 2 * dy;
            var w = (float)r.Width + 2 * dx;
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            p = new Point(x, y);
            sz = new Size(w, h);
        }
    }
}
