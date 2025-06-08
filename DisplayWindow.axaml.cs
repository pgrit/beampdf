namespace beampdf;

public partial class DisplayWindow : Window
{
    public DisplayWindow()
    {
        InitializeComponent();

        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
    }
}