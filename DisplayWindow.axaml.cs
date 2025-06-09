using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;

namespace beampdf;

public partial class DisplayWindow : Window
{
    static readonly LibVLC libvlc = new(enableDebugLogs: false);
    readonly MediaPlayer mediaPlayer = new(libvlc);

    VideoView videoView;

    public DisplayWindow()
    {
        InitializeComponent();

        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;

        // Doing this in XAML does not initialize the mediaPlayer correctly, so we add the control here...
        videoView = new()
        {
            MediaPlayer = mediaPlayer
        }; // TODO does this need to be recreated on screen switch? Maybe to be safe we just re-init the player for each video?
        VideoContainer.Children.Add(videoView);
    }

    public async void PlayVideo(string filename, double x, double y, double w)
    {
        using var media = new Media(libvlc, filename);
        await media.Parse();
        var vidTrack = media.Tracks[0].Data.Video;

        videoView.IsVisible = true;

        // Position the video view
        x = RenderTarget.Bounds.Left + x * RenderTarget.Bounds.Width;
        y = RenderTarget.Bounds.Top + y * RenderTarget.Bounds.Height;
        w = RenderTarget.Bounds.Width * w;
        videoView.Width = w;
        videoView.Height = vidTrack.Height / (double)vidTrack.Width * w;
        Canvas.SetLeft(videoView, x);
        Canvas.SetTop(videoView, y);

        mediaPlayer.Play(media);
    }

    public void StopVideo()
    {
        mediaPlayer.Stop();
        videoView.IsVisible = false;
    }
}