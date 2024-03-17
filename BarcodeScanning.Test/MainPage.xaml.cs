namespace BarcodeScanning.Test;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private async void OpenScanPageClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(ScanPage));
	}
}

