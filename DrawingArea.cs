using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace beampdf;

public class DrawingArea : Panel
{
    protected override Type StyleKeyOverride { get { return typeof(Panel); } }

    public DrawingArea()
    {
        Background = Brushes.Transparent; // Required for mouse events to trigger
        Children.Add(viewer);

        image = new(new(resX, resY));
        viewer.Source = image;
        SyncTarget?.Source = image;
    }

    int resX = 3840, resY = 2160;

    Image viewer = new();
    RenderTargetBitmap image;

    PointerPoint? lastP;

    void HandleDraw(PointerEventArgs evt, Image target)
    {
        var point = evt.GetCurrentPoint(target);
        if (point.Properties.IsLeftButtonPressed && !evt.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
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
        // TODO linearly interpolate -- place additional dots along the line to the previous position with space depending on radius
    }

    protected override void OnPointerMoved(PointerEventArgs e) => HandleDraw(e, viewer);

    public static readonly SolidColorBrush DrawColor = new(0xff127db2); // TODO shortcuts to switch color

    void Draw(Point pos, float pressure)
    {
        using (DrawingContext ctx = image.CreateDrawingContext(false))
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

        viewer.InvalidateVisual();
        SyncTarget?.InvalidateVisual();
    }

    public void Clear()
    {
        using (DrawingContext ctx = image.CreateDrawingContext(true)) { }
        viewer.InvalidateVisual();
        SyncTarget?.InvalidateVisual();
    }

    public void SetAspectRatio(double aspect)
    {
        resY = (int)(aspect * resX);
        image = new(new(resX, resY));
        viewer.Source = image;
        SyncTarget?.Source = image;
    }

    public Image SyncTarget
    {
        get;
        set
        {
            field = value;
            SyncTarget?.PointerMoved += (_, evt) =>
            {
                HandleDraw(evt, SyncTarget);
            };
        }
    }
}