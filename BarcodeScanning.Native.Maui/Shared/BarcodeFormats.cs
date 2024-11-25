namespace BarcodeScanning;

[Flags]
public enum BarcodeFormats
{
    None = 0,

    Code128 = 1 << 0,
    Code39 = 1 << 1,
    Code93 = 1 << 2,
    CodaBar = 1 << 3,
    DataMatrix = 1 << 4,
    Ean13 = 1 << 5,
    Ean8 = 1 << 6,
    Itf = 1 << 7,
    QRCode = 1 << 8,
    Upca = 1 << 9,
    Upce = 1 << 10,
    Pdf417 = 1 << 11,
    Aztec = 1 << 12,
    MicroQR = 1 << 13,
    MicroPdf417 = 1 << 14,
    I2OF5 = 1 << 15,
    GS1DataBar = 1 << 16,
    MaxiCode = 1 << 17,
    DXFilmEdge = 1 << 18,

    All = ~0
}