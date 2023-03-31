namespace BarcodeScanning.Test
{
    public partial class MainPage : ContentPage
    {
        private readonly BarcodeDrawable _drawable = new();
        private readonly List<string> qualitys = new();

        public MainPage()
        {
            InitializeComponent();

            qualitys.Add("Lowest");
            qualitys.Add("Low");
            qualitys.Add("Medium");
            qualitys.Add("High");
            qualitys.Add("Highest");

            Quality.ItemsSource = qualitys;
        }

        protected override async void OnAppearing()
        {
            await Methods.AskForRequiredPermission();
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
            public HashSet<BarcodeResult> barcodeResults;
            public void Draw(ICanvas canvas, RectF dirtyRect)
            {
                if (barcodeResults is not null && barcodeResults.Count > 0)
                {
                    foreach (var barcode in barcodeResults)
                    {
                        var drawRect = new RectF(barcode.BoundingBox.X / canvas.DisplayScale, barcode.BoundingBox.Y / canvas.DisplayScale, barcode.BoundingBox.Width / canvas.DisplayScale, barcode.BoundingBox.Height / canvas.DisplayScale);

                        canvas.StrokeColor = Colors.Red;
                        canvas.StrokeSize = 6;
                        canvas.DrawRectangle(drawRect);
                    }
                }
            }
        }
    }
}