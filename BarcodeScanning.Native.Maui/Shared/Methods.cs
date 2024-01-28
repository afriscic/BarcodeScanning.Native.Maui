namespace BarcodeScanning;

public static partial class Methods
{
    public static async Task<bool> AskForRequiredPermissionAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status == PermissionStatus.Granted)
            {
                return true;
            }
            
            status = await Permissions.RequestAsync<Permissions.Camera>();
            
            return status == PermissionStatus.Granted;
        }
        catch (Exception)
        {
            return false;
        }
    }
}