using Android.Content;
using Android.Graphics;
using Android.Widget;
using AndroidX.Camera.View;
using AndroidX.CoordinatorLayout.Widget;

using Color = Android.Graphics.Color;
using Paint = Android.Graphics.Paint;

namespace BarcodeScanning.Platforms.Android;

internal class BarcodeView : CoordinatorLayout
{
    private readonly ImageView _imageView;
    private readonly RelativeLayout _relativeLayout;

    internal BarcodeView(Context context, PreviewView previewView) : base(context)
    {
        var layoutParams = new RelativeLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
        layoutParams.AddRule(LayoutRules.CenterInParent);
        _imageView = new ImageView(context)
        {
            LayoutParameters = layoutParams
        };

        _relativeLayout = new RelativeLayout(context)
        {
            LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
        };
        _relativeLayout.AddView(previewView);
        
        this.AddView(_relativeLayout);
    }

    internal void AddAimingDot()
    {      
        var radius = 25;
        var circleBitmap = Bitmap.CreateBitmap(2 * radius, 2 * radius, Bitmap.Config.Argb8888);
        var canvas = new Canvas(circleBitmap);
        var paint = new Paint
        {
            AntiAlias = true,
            Color = Color.Red,
            Alpha = 150
        };
        canvas.DrawCircle(radius, radius, radius, paint);

        _imageView.SetImageBitmap(circleBitmap);
        _relativeLayout.AddView(_imageView);
    }

    internal void RemoveAimingDot()
    {
        try
        {
            _relativeLayout.RemoveView(_imageView);
        }
        catch (Exception)
        {
            
        }
    }
}
