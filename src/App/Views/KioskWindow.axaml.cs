using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Photobooth.App.Views;

public partial class KioskWindow : Window
{
    public KioskWindow() => AvaloniaXamlLoader.Load(this);
}
