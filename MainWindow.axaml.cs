using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MuPDF.NET;

namespace beampdf;

public partial class MainWindow : Window
{
    DisplayWindow displayWindow;

    Timer timer;

    public MainWindow()
    {
        InitializeComponent();

        KeyDown += HandleShortcuts;

        timer = new(1000.0);
        timer.Elapsed += (_, _) => UpdateTime();
        timer.Start();
        UpdateTime();

        // TODO recent file dialog with previews + Ctrl+R to open it

        // TODO gallery view to select a slide

        // TODO Zoom-in feature: ctrl+drag to select a crop, ctrl+click or RMB, or slide switch to cancel

        // TODO video playback?

        // TODO transitions: fade or switch
        // - control times, separately for pages with same label (=animations) and different labels (=transitions)
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        timer.Stop();
    }

    int pageInput = 0;
    int numPageInput = 0;

    DateTime? presentStart;

    void UpdateTime()
    {
        try
        {
            if (presentStart.HasValue)
            {
                var elapsed = DateTime.Now - presentStart;

                Dispatcher.UIThread.Invoke(() =>
                {
                    PresentTime.Text = elapsed.Value.ToString(@"hh\:mm\:ss");
                });
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    PresentTime.Text = "--:--:--";
                });
            }

            Dispatcher.UIThread.Invoke(() =>
            {
                SystemTime.Text = DateTime.Now.ToString("HH:mm");
            });
        }
        catch(TaskCanceledException)
        {
            // Occurs if the window closed between invoking this timer and dispatching the UI call => ignore
        }
    }

    void HandleShortcuts(object sender, KeyEventArgs e)
    {
        // Jumping to slides by number
        // TODO display current number input in a text field (+ hint at this feature and Enter key to confirm)
        if (int.TryParse(e.KeySymbol, out int n))
        {
            pageInput *= 10;
            pageInput += n;
            numPageInput++;
        }
        else if (numPageInput > 0 && e.Key == Key.Enter)
        {
            curPage = pageNumbers.FindLastIndex(idx => idx == pageInput);

            _ = RenderCurrentPage();
            pageInput = 0;
            numPageInput = 0;
        }
        else
        {
            pageInput = 0;
            numPageInput = 0;
        }

        if (e.Key == Key.Right)
            NextSlide();
        else if (e.Key == Key.Left)
            PreviousSlide();

        // Selecting output display
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            if (e.Key >= Key.D1 && e.Key <= Key.D9)
            {
                int i = e.Key - Key.D1;
                ShowSlides(i);
            }
        }

        if (e.Key == Key.Escape)
            CloseSlides();
        else if (e.Key == Key.F5)
            ShowSlides();

        if (e.Key == Key.Back)
            DrawingArea.Clear();
    }

    void CloseSlides()
    {
        displayWindow?.Close();
        DrawingArea.SyncTarget = null;
    }

    void ShowSlides(int screenIdx = -1)
    {
        displayWindow?.Close();
        displayWindow = new();
        displayWindow.ShowActivated = false;
        displayWindow.Show(this);

        displayWindow.KeyDown += HandleShortcuts;

        Screen screen = Screens.Primary;
        if (screenIdx < 0)
        {
            // If there is more than one screen, pick the first non-primary one
            for (int i = 0; i < Screens.ScreenCount; ++i)
            {
                if (!Screens.All[i].IsPrimary)
                {
                    screen = Screens.All[i];
                    break;
                }
            }
        }
        else if (screenIdx < Screens.ScreenCount)
        {
            screen = Screens.All[screenIdx];
        }

        displayWindow.Position = screen.WorkingArea.Position;
        displayWindow.WindowState = WindowState.FullScreen;

        DrawingArea.SyncTarget = displayWindow?.Overlay;

        // Toggling fullscreen activates the window, we undo that to retain focus
        Activate();

        displayWindow.Resized += (_, args) => {
            _ = RenderCurrentPage();
        };
        _ = RenderCurrentPage();
    }

    Document openDoc;
    List<MuPDF.NET.Label> pageLabels;
    int curPage = 0;

    void NextSlide()
    {
        curPage++;
        _ = RenderCurrentPage();
    }

    void PreviousSlide()
    {
        curPage--;
        if (curPage < 0) curPage = 0;
        _ = RenderCurrentPage();
    }

    int lastPage = -1;

    async Task RenderCurrentPage()
    {
        if (openDoc == null)
            return;

        if (curPage >= openDoc.PageCount)
            curPage = openDoc.PageCount - 1;

        if (curPage > 0 && !presentStart.HasValue)
            presentStart = DateTime.Now;

        if (displayWindow != null)
        {
            displayWindow?.RenderTarget.Source = await RenderPage(curPage, displayWindow.Width, displayWindow.Height);
        }

        var presenterBounds = PresenterRenderTarget.GetVisualParent().Bounds;
        PresenterRenderTarget.Source = await RenderPage(curPage, presenterBounds.Width, presenterBounds.Height);

        // display preview of next page
        if (curPage + 1 < openDoc.PageCount)
        {
            var previewBounds = PresenterRenderTarget.GetVisualParent().Bounds;
            PreviewRenderTarget.Source = await RenderPage(curPage + 1, previewBounds.Width, previewBounds.Height);
        }
        else
        {
            PreviewRenderTarget.Source = null;
        }

        // Update drawing area
        DrawingArea.SetAspectRatio(openDoc[curPage].Rect.Height / openDoc[curPage].Rect.Width);

        // Highlight thumbnail
        if (thumbnails != null)
        {
            if (lastPage >= 0 && lastPage < thumbnails.Length)
                thumbnails[lastPage].Background = Brushes.Transparent;
            thumbnails[curPage].Background = Brushes.Aquamarine;

            // Compute scroll position to center this slide
            double x = thumbnails[curPage].Bounds.X;
            double w = SlideStripScrollViewer.Bounds.Width;
            double offset = x - 0.5 * w; // how much to scroll to the right
            SlideStripScrollViewer.Offset = new(offset, 0);
        }
        lastPage = curPage;
    }

    List<int> pageNumbers = [];

    void ResolvePageLabels()
    {
        pageLabels = openDoc.GetPageLabels();

        if (pageLabels.Count == 0)
        {
            pageNumbers = [];
            for (int i = 0; i < openDoc.PageCount; ++i)
                pageNumbers.Add(i + 1);
            return;
        }

        // TODO not sure if sorting is required or already guaranteed...
        var sortedLabels = pageLabels.OrderBy(lbl => lbl.StartPage).ToList();
        int nextLabel = 0;

        pageNumbers = [];
        int p = 1;
        for (int i = 0; i < openDoc.PageCount; ++i)
        {
            if (nextLabel < sortedLabels.Count && i == sortedLabels[nextLabel].StartPage)
            {
                p = sortedLabels[nextLabel].FirstPageNum;
                nextLabel++;
            }

            pageNumbers.Add(p++);
        }
    }

    void ResetTimerBtn_Click(object sender, RoutedEventArgs eventArgs)
    {
        presentStart = DateTime.Now;
    }

    async void LoadPdfBtn_Click(object sender, RoutedEventArgs eventArgs)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open PDF",
            AllowMultiple = false
        });

        if (files.Count == 1)
        {
            openDoc?.Close();
            openDoc = new Document(files[0].Path.LocalPath);

            ResolvePageLabels();

            curPage = 0;
            presentStart = null;
            _ = RenderCurrentPage();

            PopulateImageStrip();
        }

        // Return focus to the parent window so our key bindings keep working
        Focus();
    }

    async Task<Bitmap> RenderPage(int page, double targetWidth, double targetHeight)
    {
        float zoomX = (float)(targetWidth * VisualRoot.RenderScaling) / openDoc[curPage].Rect.Width;
        float zoomY = (float)(targetHeight * VisualRoot.RenderScaling) / openDoc[curPage].Rect.Height;
        float zoom = float.Min(zoomX, zoomY);

        return await Task.Run(() =>
        {
            lock(openDoc)
            {
                Pixmap pixmap = openDoc[page].GetPixmap(matrix: new Matrix(zoom, zoom), colorSpace: "rgb",
                    alpha: false, annots: false);
                return new Bitmap(PixelFormats.Rgb24, AlphaFormat.Opaque, (nint)pixmap.SamplesPtr,
                    new(pixmap.W, pixmap.H), new(pixmap.Xres, pixmap.Yres), pixmap.W * 3);
            }
        });
    }

    StackPanel[] thumbnails;

    async void PopulateImageStrip()
    {
        SlideStrip.Children.Clear();
        thumbnails = null;

        List<StackPanel> panels = [];
        double size = 0.0;
        for (int i = 0; i < pageNumbers.Count; ++i)
        {
            if (i > 0 && i < pageNumbers.Count - 1 && pageNumbers[i] == pageNumbers[i + 1] )
            {
                panels.Add(panels[^1]); // Duplicate the reference for easy lookup later
                continue;
            }

            var bmp = await RenderPage(i, 96, 96);

            // shrink (or grow) height to snugly fit the biggest thumbnails
            double aspect = openDoc[curPage].Rect.Height / openDoc[curPage].Rect.Width;
            double tHeight = 96 * aspect;
            size = Math.Max(size, tHeight + 21);

            int pageNum = i;
            void jumpTo()
            {
                curPage = pageNum;
                _ = RenderCurrentPage();
            }

            StackPanel stack = new() { Margin = new(0, 0, 4, 0) };
            var img = new Image() { Width = 96, Height = 96 * aspect, Source = bmp, Cursor = new Cursor(StandardCursorType.Hand) };
            var txt = new Avalonia.Controls.TextBlock()
            {
                Width = 96,
                TextAlignment = TextAlignment.Center,
                Text = $"{pageNumbers[i]}",
                Margin = new(0, 1, 0, 0),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            img.PointerReleased += (_,_) => jumpTo();
            txt.PointerReleased += (_,_) => jumpTo();
            stack.Children.Add(img);
            stack.Children.Add(txt);
            SlideStrip.Children.Add(stack);

            panels.Add(stack);
        }
        SlideStrip.Height = size;
        (SlideStrip.Parent as Control).Height = size;
        thumbnails = [.. panels];
    }
}