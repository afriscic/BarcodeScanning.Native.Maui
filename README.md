# BarcodeScanning.Native.Maui
Barcode scanning library based on native platform APIs for barcode detection: 
1. [Google ML Kit](https://developers.google.com/ml-kit)
2. [Apple Vision framework](https://developer.apple.com/documentation/vision)

This library was inspired by existing MAUI barcode scanning libraries: [BarcodeScanner.Mobile](https://github.com/JimmyPun610/BarcodeScanner.Mobile) & [Zxing.Net.MAUI](https://github.com/Redth/ZXing.Net.Maui), but comes with many code improvements and uses native ML APIs on both Android and iOS/macOS.

## Key features
1. Uses native APIs for maximal performance and barcode readability,
2. Optimized for continuous scanning,
3. Ability to scan multiple barcodes in one frame,
4. Ability to pool multiple scans for better scanning consistency,
5. Transformed barcode bounding box for on-screen positioning,
6. From version 1.2.0 implemented `ViewfinderMode` - detect only barcodes present in camera preview on screen and `AimMode` - detect only the barcode that is overlapped with the red dot centred in camera preview,
7. From version 1.4.1 ability to control camera zoom and camera selection on supported multi-camera setups on iOS and Android,
8. From version 1.5.0 ability to save images from the camera feed.
9. Code-behind and MVVM compatibility,
10. Android only - Ability to invert source image to scan natively unsupported inverted barcodes, but at a performance cost.

## Usage
1. Install [Nuget package](https://www.nuget.org/packages/BarcodeScanning.Native.Maui),
2. Initialize the plugin in your `MauiProgram.cs`:
    ```csharp
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            ...
            .UseBarcodeScanning();
            ...

        return builder.Build();
    }
    ```
3. Add required permissions:
    #### Android
    Edit `AndroidManifest.xml` file (under the Platforms\Android folder) and add the following permissions inside of the `manifest` node:
    ```xml
    <uses-permission android:name="android.permission.CAMERA" />
    ```
    #### iOS/macOS
    Edit `info.plist` file (under the Platforms\iOS or Platforms\MacCatalyst folder) and add the following permissions inside of the `dict` node:
    ```xml
    <key>NSCameraUsageDescription</key>
    <string>Enable camera for barcode scanning.</string>
    ```
    And ask for permission from user in your code:
    ```csharp
    await Methods.AskForRequiredPermissionAsync();
    ```
4. In XAML, add correct namespace, for example:
    ```xaml
    xmlns:scanner="clr-namespace:BarcodeScanning;assembly=BarcodeScanning.Native.Maui"
    ```
5. Set the `CameraEnabled` property to `true` in XAML, code behind or ViewModel to start the camera. As a best practice set it in `OnAppearing()` method override in your ContentPage.
6. Listen to `OnDetectionFinished` event in Code-behind:
    ```xaml
    <scanner:CameraView ...
                        OnDetectionFinished="CameraView_OnDetectionFinished"
                        .../>
    ```
    ```csharp
    private void CameraView_OnDetectionFinished(object sender, OnDetectionFinishedEventArg e)
    {
        if (e.BarcodeResults.Length > 0)
            ...
    }
    ```
    or bind `OnDetectionFinishedCommand` property to a Command in your ViewModel:
    ```xaml
    <scanner:CameraView ...
                        OnDetectionFinishedCommand="{Binding DetectionFinishedCommand}"
                        .../>
    ```
    ```csharp
    public ICommand DetectionFinishedCommand { get; set; }
    ...
    DetectionFinishedCommand = new Command<BarcodeResult[]>(IReadOnlySet<BarcodeResult> result) =>
    {
        if (result.Count > 0)
            ...
    }
    ```
7. As a best practice set the `CameraEnabled` property to `false` in `OnDisappearing()` method override in your ContentPage.
8. From .NET MAUI 9 (version 2.0.0) manually calling `DisconnectHandler()` is no loger required, but optional. More info here: [What's new in .NET MAUI for .NET 9](https://learn.microsoft.com/en-us/dotnet/maui/whats-new/dotnet-9?view=net-maui-9.0#handler-disconnection)
9. From version 1.5.0 set the `CaptureNextFrame` property to `true` to capture next frame from the camera feed as a `PlatformImage`. Listen to `OnImageCaptured` event or bind to `OnImageCapturedCommand` property to get the caputured image. **Image is captured from the original camera feed and can be different from the on-screen preview.** After the image is captured `CaptureNextFrame` property is automaticly set to `false` to prevent memory leaks. Example can be found in `ScanTab.xaml.cs`.

## Supported barcode symbologies
#### Android
1D: Codabar, Code 39, Code 93, Code 128, EAN-8, EAN-13, ITF, UPC-A, UPC-E; 2D: Aztec, Data Matrix, PDF417, QR Code
#### iOS/macOS
1D: Codabar, Code 39, Code 93, Code 128, EAN-8, EAN-13, GS1 DataBar, ITF, UPC-A, UPC-E; 2D: Aztec, Data Matrix, MicroPDF417, MicroQR, PDF417, QR Code

## Bindable properties
A list of bindable properties with descriptions can be found in [CameraView.cs](https://github.com/afriscic/BarcodeScanning.Native.Maui/blob/master/BarcodeScanning.Native.Maui/CameraView.cs) source file.

## TODO Windows support
Windows is currently unsupported, but support can be added in the future through [Zxing.Net](https://github.com/micjahn/ZXing.Net) project.
