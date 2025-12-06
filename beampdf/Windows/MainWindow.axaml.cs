namespace beampdf;

public partial class MainWindow : Window
{
    DisplayWindow displayWindow;
    PdfSlide openDoc;
    Timer timer;

    public MainWindow()
    {
        InitializeComponent();

        KeyDown += HandleShortcuts;

        timer = new(1000.0);
        timer.Elapsed += (_, _) => UpdateTime();
        timer.Start();
        UpdateTime();

        // Horizontally scroll the thumbnails with the mouse wheel
        SlideStripScrollViewer.PointerWheelChanged += (sender, e) =>
        {
            var oldX = SlideStripScrollViewer.Offset.X;
            SlideStripScrollViewer.Offset = SlideStripScrollViewer.Offset.WithX(
                oldX - 96 * e.Delta.Y
            );
        };

        AddHandler(DragDrop.DropEvent, HandleDrop);
    }

    Point? cropA,
        cropB;
    bool isCropUpdating;

    void UpdateCrop()
    {
        if (cropA == null || cropB == null)
        {
            CropMarker.IsVisible = false;
            return;
        }

        // Hide the crop if it does not contain at least one pixel
        var cropSize = cropB.Value - cropA.Value;
        if (cropSize.X == 0 || cropSize.Y == 0)
            CropMarker.IsVisible = false;
        else
        {
            Canvas.SetLeft(CropMarker, Math.Min(cropA.Value.X, cropB.Value.X));
            Canvas.SetTop(CropMarker, Math.Min(cropA.Value.Y, cropB.Value.Y));
            CropMarker.Width = Math.Abs(cropSize.X);
            CropMarker.Height = Math.Abs(cropSize.Y);
            CropMarker.IsVisible = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var point = e.GetCurrentPoint(PresenterPanel);

        if (
            !point.Properties.IsRightButtonPressed
            && !(point.Properties.IsLeftButtonPressed && e.KeyModifiers == KeyModifiers.Control)
        )
        {
            if (isCropUpdating)
            {
                isCropUpdating = false;
                UpdateCrop();
            }
        }
        else
        {
            if (isCropUpdating)
            {
                cropB = point.Position;
            }
            else
            {
                cropA = point.Position;
                cropB = null; // This removes to crop until the pointer has moved
                isCropUpdating = true;
            }
            UpdateCrop();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (
            e.InitialPressMouseButton == MouseButton.Right
            || e.InitialPressMouseButton == MouseButton.Left
                && e.KeyModifiers == KeyModifiers.Control
        )
        {
            if (cropB == null || cropA == null)
            {
                _ = RenderCurrentPage();
                return;
            }

            var cropSize = cropB.Value - cropA.Value;
            if (cropSize.X == 0 || cropSize.Y == 0)
            {
                _ = RenderCurrentPage();
            }
            else
            {
                var uA =
                    (cropA.Value.X - MarkerCanvas.Bounds.Left) / MarkerCanvas.Bounds.Size.Width;
                var uB =
                    (cropB.Value.X - MarkerCanvas.Bounds.Left) / MarkerCanvas.Bounds.Size.Width;
                var vA =
                    (cropA.Value.Y - MarkerCanvas.Bounds.Top) / MarkerCanvas.Bounds.Size.Height;
                var vB =
                    (cropB.Value.Y - MarkerCanvas.Bounds.Top) / MarkerCanvas.Bounds.Size.Height;
                var uMin = Math.Min(uA, uB);
                var vMin = Math.Min(vA, vB);
                var uMax = Math.Max(uA, uB);
                var vMax = Math.Max(vA, vB);
                ShowCrop(uMin, vMin, uMax, vMax);
            }

            cropB = null;
            UpdateCrop();
        }
    }

    private void ShowCrop(double uMin, double vMin, double uMax, double vMax)
    {
        if (openDoc == null)
            return;

        // Map given relative position to page coordinate
        float x0 = (float)(openDoc[curPage].Rect.X0 + uMin * openDoc[curPage].Rect.Width);
        float x1 = (float)(openDoc[curPage].Rect.X0 + uMax * openDoc[curPage].Rect.Width);
        float y0 = (float)(openDoc[curPage].Rect.Y0 + vMin * openDoc[curPage].Rect.Height);
        float y1 = (float)(openDoc[curPage].Rect.Y0 + vMax * openDoc[curPage].Rect.Height);

        var presenterBounds = PresenterRenderTarget.GetVisualParent().Bounds;
        var w = presenterBounds.Width;
        var h = presenterBounds.Height;

        if (displayWindow != null && double.IsFinite(displayWindow.Width))
        {
            w = displayWindow.Width;
            h = displayWindow.Height;
        }
        System.Console.WriteLine(w);
        System.Console.WriteLine(h);

        var bmpT = openDoc.RenderPage(
            curPage,
            new(x0, y0, x1, y1),
            presenterBounds.Width,
            presenterBounds.Height,
            VisualRoot.RenderScaling
        );
        bmpT.Wait();
        var bmp = bmpT.Result;

        if (displayWindow != null)
            displayWindow.RenderTarget.Source = bmp;
        PresenterRenderTarget.Source = bmp;
    }

    private void HandleDrop(object sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files == null)
            return;

        // Load the first file that is valid
        foreach (var file in files)
        {
            if (File.Exists(file.Path.LocalPath))
            {
                _ = OpenDocument(file.Path.LocalPath);
                break;
            }
        }
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
        catch (TaskCanceledException)
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
            curPage = openDoc.GetLastPageOfSlide(pageInput);

            _ = RenderCurrentPage();
            pageInput = 0;
            numPageInput = 0;
        }
        else
        {
            pageInput = 0;
            numPageInput = 0;
        }

        if (e.Key == Key.Right || e.Key == Key.Next)
            NextSlide();
        else if (e.Key == Key.Left || e.Key == Key.Prior)
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

        if (e.Key == Key.OemPeriod)
            displayWindow?.RestartVideo();
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

        // Only toggle fullscreen once we're sure the Window is positioned
        // This is required on Linux, without it fullscreen just uses a
        // random screen
        displayWindow.Resized += (_, _) =>
        {
            displayWindow.WindowState = WindowState.FullScreen;
        };

        DrawingArea.SyncTarget = displayWindow?.Overlay;

        // Toggling fullscreen activates the window, we undo that to retain focus
        Activate();

        displayWindow.Resized += (_, args) =>
        {
            _ = RenderCurrentPage();
        };
        _ = RenderCurrentPage();
    }

    int curPage = 0;

    void NextSlide()
    {
        curPage++;
        _ = RenderCurrentPage();
    }

    void PreviousSlide()
    {
        curPage--;
        if (curPage < 0)
            curPage = 0;
        _ = RenderCurrentPage();
    }

    int lastPage = -1;

    public static readonly SolidColorBrush ThumbHighlightColor = new(0xffd3642d);

    async Task RenderCurrentPage()
    {
        if (openDoc == null)
            return;

        if (curPage >= openDoc.NumPages)
            curPage = openDoc.NumPages - 1;

        if (curPage > 0 && !presentStart.HasValue)
            presentStart = DateTime.Now;

        if (displayWindow != null && double.IsFinite(displayWindow.Width))
        {
            displayWindow.RenderTarget.Source = await openDoc.RenderPage(
                curPage,
                null,
                displayWindow.Width,
                displayWindow.Height,
                VisualRoot.RenderScaling
            );
        }

        var presenterBounds = PresenterRenderTarget.GetVisualParent().Bounds;
        PresenterRenderTarget.Source = await openDoc.RenderPage(
            curPage,
            null,
            presenterBounds.Width,
            presenterBounds.Height,
            VisualRoot.RenderScaling,
            true
        );

        // display preview of next page
        if (curPage + 1 < openDoc.NumPages)
        {
            var previewBounds = PreviewRenderTarget.GetVisualParent().Bounds;
            PreviewRenderTarget.Source = await openDoc.RenderPage(
                curPage + 1,
                null,
                previewBounds.Width,
                previewBounds.Height,
                VisualRoot.RenderScaling
            );
        }
        else
        {
            PreviewRenderTarget.Source = null;
        }

        // Update drawing area
        var aspect = openDoc[curPage].Rect.Height / openDoc[curPage].Rect.Width;
        DrawingArea.SetAspectRatio(aspect);
        MarkerCanvas.Height = aspect * MarkerCanvas.Bounds.Width;

        // Highlight thumbnail
        if (thumbnails != null)
        {
            if (lastPage >= 0 && lastPage < thumbnails.Length)
                thumbnails[lastPage].Background = Brushes.Transparent;
            thumbnails[curPage].Background = ThumbHighlightColor;

            // Compute scroll position to center this slide
            double x = thumbnails[curPage].Bounds.X;
            double w = SlideStripScrollViewer.Bounds.Width;
            double offset = x - 0.5 * w; // how much to scroll to the right
            SlideStripScrollViewer.Offset = new(offset, 0);
        }
        lastPage = curPage;

        // Display speaker notes
        SpeakerNotes.Text = openDoc.TryGetSpeakerNote(curPage);

        // If there is a video here, play it
        if (displayWindow != null)
        {
            var vid = openDoc.TryGetVideo(curPage);
            if (vid.HasValue)
            {
                double x = vid.Value.X / openDoc[curPage].Rect.Width;
                double y = vid.Value.Y / openDoc[curPage].Rect.Height;
                double w = vid.Value.W / openDoc[curPage].Rect.Width;
                displayWindow.PlayVideo(vid.Value.Filename, x, y, w, vid.Value.IsLoop);
            }
            else
            {
                displayWindow.StopVideo();
            }
        }
    }

    void ResetTimerBtn_Click(object sender, RoutedEventArgs eventArgs)
    {
        presentStart = DateTime.Now;
    }

    async void LoadRecentBtn_Click(object sender, RoutedEventArgs eventArgs)
    {
        var picker = new RecentFilePicker();
        await picker.ShowDialog(this);
        if (picker.SelectedFilename != null)
        {
            await OpenDocument(picker.SelectedFilename);
        }
    }

    async Task OpenDocument(string filename)
    {
        openDoc?.Dispose();
        openDoc = new(filename);

        RecentFilePicker.AddFile(filename, DateTime.Now);

        curPage = 0;
        await RenderCurrentPage();

        PopulateImageStrip();
    }

    async void LoadPdfBtn_Click(object sender, RoutedEventArgs eventArgs)
    {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions { Title = "Open PDF", AllowMultiple = false }
        );

        if (files.Count == 1)
        {
            _ = OpenDocument(files[0].Path.LocalPath);
        }

        // Return focus to the parent window so our key bindings keep working
        Focus();
    }

    StackPanel[] thumbnails;

    async void PopulateImageStrip()
    {
        SlideStrip.Children.Clear();
        thumbnails = null;

        List<StackPanel> panels = [];
        double size = 0.0;
        for (int i = 0; i < openDoc.NumPages; ++i)
        {
            if (
                i > 0
                && i < openDoc.NumPages - 1
                && openDoc.GetSlideNumber(i) == openDoc.GetSlideNumber(i + 1)
            )
            {
                panels.Add(panels[^1]); // Duplicate the reference for easy lookup later
                continue;
            }

            var bmp = await openDoc.RenderPage(i, null, 96, 96, VisualRoot.RenderScaling);

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
            var img = new Image()
            {
                Width = 96,
                Height = 96 * aspect,
                Source = bmp,
                Cursor = new Cursor(StandardCursorType.Hand),
            };
            var txt = new Avalonia.Controls.TextBlock()
            {
                Width = 96,
                TextAlignment = TextAlignment.Center,
                Text = $"{openDoc.GetSlideNumber(i)}",
                Margin = new(0, 1, 0, 0),
                Cursor = new Cursor(StandardCursorType.Hand),
            };
            img.PointerReleased += (_, _) => jumpTo();
            txt.PointerReleased += (_, _) => jumpTo();
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
