namespace BarcodeScanning;

[Flags]
public enum BarcodeFormats
{
    None = 0,
    Code128 = 1,
    Code39 = 2,
    Code93 = 4,
    CodaBar = 8,
    DataMatrix = 16,
    Ean13 = 32,
    Ean8 = 64,
    Itf = 128,
    QRCode = 256,
    Upca = 512,
    Upce = 1024,
    Pdf417 = 2048,
    Aztec = 4096,
    MicroQR = 8192,
    MicroPdf417 = 16384,
    I2OF5 = 32768,
    GS1DataBar = 65536,
    MaxiCode = 131072,
    DXFilmEdge = 262144,
    All = 524288
}
