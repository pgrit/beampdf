using System.Threading;
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;

namespace beampdf;

public partial class DisplayWindow : Window
{
    static LibVLC Libvlc
    {
        get
        {
            field ??= new(enableDebugLogs: false);
            return field;
        }
    }
    MediaPlayer mediaPlayer;

    VideoView videoView;

    bool isLooping = false;
    string curFilename;

    public DisplayWindow()
    {
        InitializeComponent();

        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;

        SleepInhibitor.Inhibit();
    }

    void AddVideoPlayer()
    {
        // Doing this in XAML does not initialize the mediaPlayer correctly, so we add the control here...
        mediaPlayer = new(Libvlc);
        videoView = new() { MediaPlayer = mediaPlayer };
        VideoContainer.Children.Add(videoView);

        mediaPlayer.EndReached += (_, _) =>
        {
            if (isLooping)
            {
                ThreadPool.QueueUserWorkItem(
                    (_) =>
                    {
                        RestartVideo();
                    }
                );
            }
            else // if it's not a loop, we are no longer playing this file
                curFilename = null;
        };
    }

    public async void PlayVideo(string filename, double x, double y, double w, bool loop = false)
    {
        isLooping = loop;

        // If the same video is played again, just keep going
        if (filename == curFilename)
            return;
        curFilename = filename;

        if (videoView == null)
            AddVideoPlayer();

        using var media = new Media(Libvlc, filename);
        await media.Parse();
        var vidTrack = media.Tracks[0].Data.Video;


        // Position the video view
        x = RenderTarget.Bounds.Left + x * RenderTarget.Bounds.Width;
        y = RenderTarget.Bounds.Top + y * RenderTarget.Bounds.Height;
        w = RenderTarget.Bounds.Width * w;
        videoView.Width = w;
        videoView.Height = vidTrack.Height / (double)vidTrack.Width * w;
        Canvas.SetLeft(videoView, x);
        Canvas.SetTop(videoView, y);

        // Hide the media player until it actually starts playing
        // The "Playing" event fires the moment we call Play()
        // So we use PositionChanged instead, which fires once the first frame is shown
        videoView.IsVisible = false;
        mediaPlayer.PositionChanged += (_, _) =>
            Dispatcher.UIThread.Invoke(() =>
            {
                videoView.IsVisible = true;
            });

        mediaPlayer.Play(media);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Explicitly ditch the media player, else libVLC will automatically create
        // a new window and start playback there.
        mediaPlayer = null;
    }

    public void RestartVideo()
    {
        mediaPlayer?.Stop();
        mediaPlayer?.Play();
    }

    public void StopVideo()
    {
        mediaPlayer?.Stop();
        if (videoView != null)
            videoView.IsVisible = false;
        curFilename = null;
    }
}
