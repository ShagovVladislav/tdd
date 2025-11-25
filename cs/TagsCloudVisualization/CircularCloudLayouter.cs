using System.Drawing;

namespace TagsCloudVisualization;

public class CircularCloudLayouter
{
    public readonly Point Center;
    public readonly List<Rectangle> PlacedRectangles = [];
    private readonly Spiral spiral;

    public CircularCloudLayouter(Point center)
    {
        Center = center;
        spiral = new Spiral(center);
    }

    public Rectangle PutNextRectangle(Size rectangleSize)
    {
        var rect = FindFreeRectangle(rectangleSize);
        rect = ShiftToCenter(rect);
        
        PlacedRectangles.Add(rect);
        return rect;
    }

    private Rectangle FindFreeRectangle(Size size)
    {
        while (true)
        {
            var point = spiral.GetNextPoint();
            var rect = CreateRectangleByCenter(point, size);

            if (!IntersectsWithOthers(rect))
                return rect;
        }
    }
    
    private Rectangle ShiftToCenter(Rectangle rect)
    {
        rect = ShiftAxis(rect, 
            dx: rect.X < Center.X ? 1 : -1,
            dy: 0);

        rect = ShiftAxis(rect, 
            dx: 0,
            dy: rect.Y < Center.Y ? 1 : -1);

        return rect;
    }

    private Rectangle ShiftAxis(Rectangle rect, int dx, int dy)
    {
        while (true)
        {
            var shifted = rect with { X = rect.X + dx, Y = rect.Y + dy };

            if (IntersectsWithOthers(shifted) || TouchesCorner(rect, shifted) || CrossedTheCenter(rect, dx, dy, shifted))
                return rect;

            rect = shifted;
        }
    }

    private bool TouchesCorner(Rectangle oldRect, Rectangle newRect)
    {
        return PlacedRectangles.Any(r => !oldRect.IntersectsWith(r) && newRect.IntersectsWith(r));
    }

    private bool CrossedTheCenter(Rectangle oldRect, int dx, int dy, Rectangle shifted)
    {
        var oldCx = oldRect.X + oldRect.Width / 2;
        var oldCy = oldRect.Y + oldRect.Height / 2;

        var newCx = shifted.X + shifted.Width / 2;
        var newCy = shifted.Y + shifted.Height / 2;

        if (dx != 0)
            return Math.Abs(newCx - Center.X) > Math.Abs(oldCx - Center.X);

        if (dy != 0)
            return Math.Abs(newCy - Center.Y) > Math.Abs(oldCy - Center.Y);

        return false;
    }

    private bool IntersectsWithOthers(Rectangle rect)
        => PlacedRectangles.Any(r => r.IntersectsWith(rect with{ Width = rect.Width+1, Height = rect.Height+1}));

    private static Rectangle CreateRectangleByCenter(Point center, Size size)
        => new(
            center.X - size.Width / 2,
            center.Y - size.Height / 2,
            size.Width,
            size.Height);
}
