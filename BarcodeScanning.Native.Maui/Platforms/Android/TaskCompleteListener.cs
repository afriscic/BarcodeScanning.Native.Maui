using Android.Gms.Tasks;

namespace BarcodeScanning.Platforms.Android;

internal class TaskCompleteListener : Java.Lang.Object, IOnCompleteListener
{
    private readonly TaskCompletionSource<Java.Lang.Object> _taskCompletionSource;

    internal TaskCompleteListener(TaskCompletionSource<Java.Lang.Object> tcs)
    {
        _taskCompletionSource = tcs;
    }

    public void OnComplete(global::Android.Gms.Tasks.Task task)
    {
        if (task.IsCanceled)
            _taskCompletionSource.SetCanceled();
        else if (task.IsSuccessful)
            _taskCompletionSource.SetResult(task.Result);
        else
            _taskCompletionSource.SetException(task.Exception);
    }
}
