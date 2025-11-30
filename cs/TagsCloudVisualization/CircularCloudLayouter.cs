using System.Drawing;

namespace TagsCloudVisualization;

public class CircularCloudLayouter
{
    private readonly Point center;
    private readonly List<Rectangle> placedRectangles = [];
    public IReadOnlyList<Rectangle> PlacedRectangles => placedRectangles;
    private readonly Spiral spiral;

    public CircularCloudLayouter(Point center)
    {
        this.center = center;
        spiral = new Spiral(center);
    }

    public Rectangle PutNextRectangle(Size rectangleSize)
    {
        if (rectangleSize.Width <= 0 || rectangleSize.Height <= 0)
            throw new ArgumentException("Rectangle size must be positive", nameof(rectangleSize));
        
        var rect = FindFreeSpaceForRectangle(rectangleSize);
        rect = ShiftToCenter(rect);
        
        placedRectangles.Add(rect);
        return rect;
    }

    private Rectangle FindFreeSpaceForRectangle(Size size)
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
            dx: rect.X < center.X ? 1 : -1,
            dy: 0);

        rect = ShiftAxis(rect, 
            dx: 0,
            dy: rect.Y < center.Y ? 1 : -1);

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
        var oldCenterX = oldRect.X + oldRect.Width / 2;
        var oldCenterY = oldRect.Y + oldRect.Height / 2;

        var newCenterX = shifted.X + shifted.Width / 2;
        var newCenterY = shifted.Y + shifted.Height / 2;

        if (dx != 0)
            return Math.Abs(newCenterX - center.X) > Math.Abs(oldCenterX - center.X);

        if (dy != 0)
            return Math.Abs(newCenterY - center.Y) > Math.Abs(oldCenterY - center.Y);

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
