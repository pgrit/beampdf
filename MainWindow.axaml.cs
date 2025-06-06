using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using MuPDF.NET;

namespace beampdf;

public partial class MainWindow : Window
{
    DisplayWindow displayWindow;

    public MainWindow()
    {
        InitializeComponent();

        KeyDown += HandleShortcuts;


        // TODO display timer + reset feature

        // TODO screen selection: specify screen number for slides view (presenter can be dragged, ez)

        // TODO recent file dialog with previews + Ctrl+R to open it

        // TODO video playback?
    }

    int pageInput = 0;
    int numPageInput = 0;

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

            RenderCurrentPage();
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
            RenderCurrentPage();
        };
        RenderCurrentPage();
    }

    Document openDoc;
    List<MuPDF.NET.Label> pageLabels;
    int curPage = 0;

    void NextSlide()
    {
        curPage++;
        RenderCurrentPage();
    }

    void PreviousSlide()
    {
        curPage--;
        if (curPage < 0) curPage = 0;
        RenderCurrentPage();
    }

    void RenderCurrentPage()
    {
        if (openDoc == null)
            return;

        if (curPage >= openDoc.PageCount)
            curPage = openDoc.PageCount - 1;

        float targetWidth = (float)((displayWindow?.Width ?? Width) * VisualRoot.RenderScaling);
        float zoomX = targetWidth / openDoc[curPage].Rect.Width;
        float targetHeight = (float)((displayWindow?.Height ?? Height) * VisualRoot.RenderScaling);
        float zoomY = targetHeight / openDoc[curPage].Rect.Height;
        float zoom = float.Min(zoomX, zoomY);

        Pixmap pixmap = openDoc[curPage].GetPixmap(matrix: new MuPDF.NET.Matrix(zoom, zoom), colorSpace: "rgb", alpha: false, annots: false);
        var bmp = new Bitmap(PixelFormats.Rgb24, AlphaFormat.Opaque,
            (nint)pixmap.SamplesPtr, new(pixmap.W, pixmap.H), new(pixmap.Xres, pixmap.Yres), pixmap.W * 3);
        PresenterRenderTarget.Source = bmp;

        if (displayWindow != null)
            displayWindow.RenderTarget.Source = bmp;

        // display preview of next page
        if (curPage + 1 < openDoc.PageCount)
        {
            pixmap = openDoc[curPage+1].GetPixmap(matrix: new MuPDF.NET.Matrix(zoom, zoom), colorSpace: "rgb", alpha: false, annots: false);
            bmp = new Bitmap(PixelFormats.Rgb24, AlphaFormat.Opaque,
                (nint)pixmap.SamplesPtr, new(pixmap.W, pixmap.H), new(pixmap.Xres, pixmap.Yres), pixmap.W * 3);
            PreviewRenderTarget.Source = bmp;
        }
        else
        {
            PreviewRenderTarget.Source = null;
        }

        // Update drawing area
        DrawingArea.SetAspectRatio(openDoc[curPage].Rect.Height / openDoc[curPage].Rect.Width);
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

    async void LoadPdfBtn_Click(object sender, RoutedEventArgs eventArgs)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open PDF",
            AllowMultiple = false
        });

        if (files.Count == 1)
        {
            openDoc = new Document(files[0].Path.LocalPath);

            ResolvePageLabels();

            curPage = 0;
            RenderCurrentPage();
        }

        // Return focus to the parent window so our key bindings keep working
        Focus();
    }
}