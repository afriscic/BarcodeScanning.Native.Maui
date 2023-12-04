using Android.Content;
using Android.Widget;
using AndroidX.Camera.View;
using AndroidX.CoordinatorLayout.Widget;

namespace BarcodeScanning.Platforms.Android;

internal class BarcodeView : CoordinatorLayout
{
    public BarcodeView(Context context, PreviewView previewView) : base(context)
    {
        var relativeLayout = new RelativeLayout(context)
        {
            LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
        };
        relativeLayout.AddView(previewView);
        this.AddView(relativeLayout);
    }
}
