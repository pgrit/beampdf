using Avalonia.Controls;

namespace beampdf;

public partial class DisplayWindow : Window
{
    public DisplayWindow()
    {
        InitializeComponent();

        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
    }
}