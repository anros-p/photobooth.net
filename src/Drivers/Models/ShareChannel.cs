namespace Photobooth.Drivers.Models;

[Flags]
public enum ShareChannel
{
    None = 0,
    Email = 1,
    Sms = 2,
    QrCode = 4,
    Microsite = 8
}
