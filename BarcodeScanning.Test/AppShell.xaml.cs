namespace BarcodeScanning.Test;

public partial class AppShell : Shell
{
	public AppShell()
	{
		Routing.RegisterRoute(nameof(ScanPage), typeof(ScanPage));

		InitializeComponent();
	}
}
