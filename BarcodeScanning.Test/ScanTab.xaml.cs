using Microsoft.Maui.Graphics.Platform;

namespace BarcodeScanning.Test
{
    public partial class ScanTab : ContentPage
    {
        private readonly BarcodeDrawable _drawable = new();
        private readonly List<string> qualitys = new();

        public ScanTab()
        {
            InitializeComponent();

            qualitys.Add("Low");
            qualitys.Add("Medium");
            qualitys.Add("High");
            qualitys.Add("Highest");

            Quality.ItemsSource = qualitys;
            if (DeviceInfo.Platform != DevicePlatform.MacCatalyst)
                Quality.Title = "Quality";
        }

        protected override async void OnAppearing()
        {
            await Methods.AskForRequiredPermissionAsync();
            base.OnAppearing();

            Barcode.CameraEnabled = true;
            Graphics.Drawable = _drawable;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Barcode.CameraEnabled = false;
        }

        private void CameraView_OnDetectionFinished(object sender, OnDetectionFinishedEventArg e)
        {
            _drawable.barcodeResults = e.BarcodeResults;

            if (e.BarcodeResults.Length > 0)
                Barcode.CaptureNextFrame = true;

            Graphics.Invalidate();
        }

        private void CameraView_OnImageCaptured(object sender, OnImageCapturedEventArg e)
        {
            _drawable.image?.Dispose();
            _drawable.image = e.Image;

            Graphics.Invalidate();
        }

        private void CameraButton_Clicked(object sender, EventArgs e)
        {
            if (Barcode.CameraFacing == CameraFacing.Back)
                Barcode.CameraFacing = CameraFacing.Front;
            else
                Barcode.CameraFacing = CameraFacing.Back;
        }

        private void TorchButton_Clicked(object sender, EventArgs e)
        {
            if (Barcode.TorchOn)
                Barcode.TorchOn = false;
            else
                Barcode.TorchOn = true;
        }

        private void VibrateButton_Clicked(object sender, EventArgs e)
        {
            if (Barcode.VibrationOnDetected)
                Barcode.VibrationOnDetected = false;
            else
                Barcode.VibrationOnDetected = true;
        }

        private void PauseButton_Clicked(object sender, EventArgs e)
        {
            if (Barcode.PauseScanning)
                Barcode.PauseScanning = false;
            else
                Barcode.PauseScanning = true;
        }

        private void Quality_SelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            if(picker.SelectedIndex > -1 && picker.SelectedIndex < 5)
                Barcode.CaptureQuality = (CaptureQuality)picker.SelectedIndex;
        }

        private class BarcodeDrawable : IDrawable
        {
            public BarcodeResult[]? barcodeResults;
            public PlatformImage? image;
            public void Draw(ICanvas canvas, RectF dirtyRect)
            {
                if (image is not null)
                {
                    canvas.DrawImage(image, 10, 10, 200, 200);
                }

                if (barcodeResults is not null && barcodeResults.Length > 0)
                {
                    canvas.StrokeSize = 15;
                    canvas.StrokeColor = Colors.Red;
                    var scale = 1 / canvas.DisplayScale;
                    canvas.Scale(scale, scale);

                    foreach (var barcode in barcodeResults)
                    {
                        canvas.DrawRectangle(barcode.PreviewBoundingBox);
                    }
                }
            }
        }
    }
}