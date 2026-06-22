using System.Diagnostics;

namespace beampdf;

public class DrawingArea : Panel
{
    protected override Type StyleKeyOverride
    {
        get { return typeof(Panel); }
    }

    Timer timer;

    public DrawingArea()
    {
        Background = Brushes.Transparent; // Required for mouse events to trigger
        Children.Add(image);

        bitmap = new(new(resX, resY));
        image.Source = bitmap;
        SyncTarget?.Source = bitmap;

        // Setup a timer to fade the laser
        timer = new(LaserTimeMs / 10000.0); // 10 ticks per interval
        timer.Elapsed += (_, _) => UpdateLaser();
        timer.Start();
    }

    int resX = 3840,
        resY = 2160;

    Image image = new();
    RenderTargetBitmap bitmap;

    PointerPoint? lastP;

    void HandleDraw(PointerEventArgs evt, Image target)
    {
        var point = evt.GetCurrentPoint(target);
        if (point.Properties.IsLeftButtonPressed && !evt.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            // TODO the !Ctrl is a hard-coded check to avoid conflict with crop selection
            //      this should be handled in a more central way

            // Map position to that of the render target
            double scaleX = target.Bounds.Width / resX;
            double scaleY = target.Bounds.Height / resY;
            Point scaledPos = new(point.Position.X / scaleX, point.Position.Y / scaleY);

            Draw(scaledPos, point.Properties.Pressure);

            if (lastP != null)
            {
                var dist = Point.Distance(point.Position, lastP.Value.Position);
                const double threshold = 0.1;
                if (dist > threshold)
                {
                    // Interpolate between the positions if motion exceeded the minimum
                    int numSplats = (int)(dist / threshold);
                    for (int i = 1; i < numSplats - 1; ++i)
                    {
                        var d = point.Position - lastP.Value.Position;
                        Vector dir = new(d.X / dist, d.Y / dist);
                        var p = lastP.Value.Position + dir * i * threshold;

                        scaledPos = new(p.X / scaleX, p.Y / scaleY);
                        Draw(scaledPos, point.Properties.Pressure);
                    }
                }
            }

            lastP = point;
        }
        else
        {
            lastP = null;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e) => HandleDraw(e, image);

    protected override void OnPointerPressed(PointerPressedEventArgs e) => HandleDraw(e, image);

    public uint PenColor { get; set; } = 0xff127db2;
    public bool IsLaserMode { get; set; } = false;

    /// <summary>
    /// Time in milliseconds after the last "draw" event to clear the laser strokes
    /// </summary>
    public readonly int LaserTimeMs = 1000;

    Stopwatch laserTimer = new();

    void Draw(Point pos, float pressure)
    {
        SolidColorBrush DrawColor = new(PenColor);

        using (DrawingContext ctx = bitmap.CreateDrawingContext(false))
        {
            new GeometryDrawing()
            {
                Geometry = new EllipseGeometry()
                {
                    Center = pos,
                    RadiusX = pressure * 10,
                    RadiusY = pressure * 10,
                },
                Brush = DrawColor,
            }.Draw(ctx);
        }
        image.InvalidateVisual();
        SyncTarget?.InvalidateVisual();

        lock (laserTimer)
            laserTimer.Restart();
    }

    public void Clear()
    {
        using (DrawingContext ctx = bitmap.CreateDrawingContext(true)) { }
        image.InvalidateVisual();
        SyncTarget?.InvalidateVisual();
    }

    private void UpdateLaser()
    {
        // The timer does not run in the UI thread, so we need to sync
        lock (laserTimer)
        {
            if (!IsLaserMode)
                return;

            if (laserTimer.ElapsedMilliseconds < LaserTimeMs)
                return;

            laserTimer.Reset();

            Dispatcher.UIThread.Invoke(Clear);

            // TODO should we keep laser strokes in a separate image?
        }
    }

    public void SetAspectRatio(double aspect)
    {
        resY = (int)(aspect * resX);
        bitmap = new(new(resX, resY));
        image.Source = bitmap;
        SyncTarget?.Source = bitmap;
    }

    public Image SyncTarget
    {
        get;
        set
        {
            field = value;
            if (field == null)
                return;
            field.PointerMoved += (_, evt) => HandleDraw(evt, SyncTarget);
        }
    }
}
