using System.Drawing;

namespace TagsCloudVisualization;

public sealed class CircularCloudLayouter
{
    private readonly Point center;
    private readonly int centerX;
    private readonly int centerY;
    private readonly List<Rectangle> placedRectangles = [];
    public IReadOnlyList<Rectangle> PlacedRectangles => placedRectangles;
    private readonly Spiral spiral;
    private readonly Dictionary<(int gx, int gy), List<Rectangle>> grid = new();
    private readonly List<Rectangle> cellBuffer = new(128); 
    private const int CellSize = 60;
    private readonly List<Rectangle> candidatesBuffer = new(1024);
    private readonly HashSet<int> seenHashes = [];
    private const int MaxTouchNeighbors = 300; 
    private const int MaxCandidatesToEvaluate = 800;
    private const int PullMaxIterations = 1800;
    private const int BigStep = 16;

    public CircularCloudLayouter(Point center)
    {
        this.center = center;
        centerX = center.X;
        centerY = center.Y;
        spiral = new Spiral(center);
    }

    public Rectangle PutNextRectangle(Size rectangleSize)
    {
        if (rectangleSize.Width <= 0 || rectangleSize.Height <= 0)
            throw new ArgumentException("Rectangle size must be positive");

        var rect = FindFreeSpaceForRectangle(rectangleSize);
        rect = ShiftToCenter(rect);

        placedRectangles.Add(rect);
        AddToGrid(rect);
        return rect;
    }
    
    private Rectangle FindFreeSpaceForRectangle(Size size)
    {
        if (placedRectangles.Count == 0)
            return CreateRectangleByCenter(center, size);

        candidatesBuffer.Clear();
        seenHashes.Clear();

        foreach (var neighbor in GetNearbyRectangles())
        {
            foreach (var c in GetTouchCandidates(neighbor, size))
            {
                AddInHashAndCandidates(c);
            }
        }

        if (candidatesBuffer.Count == 0)
        {
            foreach (var c in GetSpiralCandidates(size, 40))
            {
                AddInHashAndCandidates(c);
            }
        }

        if (candidatesBuffer.Count > MaxCandidatesToEvaluate)
        {
            candidatesBuffer.Sort((a, b) => DistanceToCenterInt(a).CompareTo(DistanceToCenterInt(b)));
            candidatesBuffer.RemoveRange(MaxCandidatesToEvaluate, candidatesBuffer.Count - MaxCandidatesToEvaluate);
        }

        var best = Rectangle.Empty;
        var bestDist = double.MaxValue;
        foreach (var c in candidatesBuffer)
        {
            if (IntersectsGrid(c)) continue;
            var d = DistanceToCenterInt(c);
            
            if (!(d < bestDist)) continue;
            bestDist = d;
            best = c;
        }

        return Math.Abs(bestDist - double.MaxValue) < 0.00000001 ? FindBySpiralFallback(size) : best;
    }

    private void AddInHashAndCandidates(Rectangle c)
    {
        var h = HashRect(c);
        if (seenHashes.Add(h))
            candidatesBuffer.Add(c);
    }

    private IEnumerable<Rectangle> GetNearbyRectangles()
    {
        var yielded = 0;

        const int radiusCells = 6;
        var cx = centerX / CellSize;
        var cy = centerY / CellSize;

        for (var dx = -radiusCells; dx <= radiusCells && yielded < MaxTouchNeighbors; dx++)
        {
            for (var dy = -radiusCells; dy <= radiusCells && yielded < MaxTouchNeighbors; dy++)
            {
                if (!grid.TryGetValue((cx + dx, cy + dy), out var list)) continue;
                for (var i = list.Count - 1; i >= 0 && yielded < MaxTouchNeighbors; i--)
                {
                    yield return list[i];
                    yielded++;
                }
            }
        }

        if (yielded < MaxTouchNeighbors && placedRectangles.Count > 0)
        {
            var last = placedRectangles[^1];
            foreach (var r in GetRectanglesAround(last, maxCells: 4))
            {
                yield return r;
                yielded++;
                if (yielded >= MaxTouchNeighbors) yield break;
            }
        }

        if (yielded >= MaxTouchNeighbors) yield break;
        {
            for (var i = placedRectangles.Count - 1; i >= 0 && yielded < MaxTouchNeighbors; i--)
            {
                yield return placedRectangles[i];
                yielded++;
            }
        }
    }

    private IEnumerable<Rectangle> GetRectanglesAround(Rectangle r, int maxCells)
    {
        var x1 = Math.Max((r.Left / CellSize) - maxCells, -1000000);
        var x2 = Math.Min((r.Right / CellSize) + maxCells, 1000000);
        var y1 = Math.Max((r.Top / CellSize) - maxCells, -1000000);
        var y2 = Math.Min((r.Bottom / CellSize) + maxCells, 1000000);

        for (var x = x1; x <= x2; x++)
        for (var y = y1; y <= y2; y++)
            if (grid.TryGetValue((x, y), out var list))
                foreach (var rr in list)
                    yield return rr;
    }

    private static IEnumerable<Rectangle> GetTouchCandidates(Rectangle o, Size size)
    {
        // left
        yield return new Rectangle(
            o.Left - size.Width,
            o.Top + ((o.Height - size.Height) >> 1),
            size.Width, size.Height);

        // right
        yield return new Rectangle(
            o.Right,
            o.Top + ((o.Height - size.Height) >> 1),
            size.Width, size.Height);

        // top
        yield return new Rectangle(
            o.Left + ((o.Width - size.Width) >> 1),
            o.Top - size.Height,
            size.Width, size.Height);

        // bottom
        yield return new Rectangle(
            o.Left + ((o.Width - size.Width) >> 1),
            o.Bottom,
            size.Width, size.Height);
    }

    private Rectangle ShiftToCenter(Rectangle rect)
    {
        var dx = (rect.X + (rect.Width >> 1) < centerX) ? +1 : -1;
        var dy = (rect.Y + (rect.Height >> 1) < centerY) ? +1 : -1;

        rect = ShiftAxis(rect, dx, 0);
        rect = ShiftAxis(rect, 0, dy);

        rect = PullTowardsCenter(rect);

        return rect;
    }

    private Rectangle ShiftAxis(Rectangle rect, int dx, int dy)
    {
        if (dx == 0 && dy == 0) return rect;

        for (var i = 0; i < 1000; i++)
        {
            var shifted = rect with { X = rect.X + dx * BigStep, Y = rect.Y + dy * BigStep };
            if (IntersectsGrid(shifted) || CrossedCenter(rect, shifted, dx, dy))
                break;
            rect = shifted;
        }

        for (var i = 0; i < 3000; i++)
        {
            var shifted = rect with { X = rect.X + dx, Y = rect.Y + dy };
            if (IntersectsGrid(shifted) || CrossedCenter(rect, shifted, dx, dy))
                return rect;
            rect = shifted;
        }

        return rect;
    }

    private Rectangle PullTowardsCenter(Rectangle rect)
    {
        var iter = 0;
        var moved = true;

        while (moved && iter++ < PullMaxIterations)
        {
            moved = false;
            var dx = rect.X + (rect.Width >> 1) < centerX ? -1 : 1;
            var dy = rect.Y + (rect.Height >> 1) < centerY ? -1 : 1;

            var leftRight = rect with { X = rect.X - dx };
            if (!IntersectsGrid(leftRight))
            {
                rect = leftRight;
                moved = true;
            }

            var upDown = rect with { Y = rect.Y - dy };
            if (IntersectsGrid(upDown)) continue;
            rect = upDown;
            moved = true;
        }

        return rect;
    }

    private bool CrossedCenter(Rectangle oldRect, Rectangle newRect, int dx, int dy)
    {
        var oldCx = oldRect.X + oldRect.Width / 2.0;
        var oldCy = oldRect.Y + oldRect.Height / 2.0;

        var newCx = newRect.X + newRect.Width / 2.0;
        var newCy = newRect.Y + newRect.Height / 2.0;

        if (dx != 0)
            return Math.Abs(newCx - centerX) > Math.Abs(oldCx - centerX);

        if (dy != 0)
            return Math.Abs(newCy - centerY) > Math.Abs(oldCy - centerY);

        return false;
    }

    private void AddToGrid(Rectangle r)
    {
        foreach (var cell in GetCells(r))
        {
            if (!grid.TryGetValue(cell, out var list))
                grid[cell] = list = new List<Rectangle>(4);
            list.Add(r);
        }
    }

    private bool IntersectsGrid(Rectangle rect)
    {
        cellBuffer.Clear();
        foreach (var cell in GetCells(rect))
        {
            if (grid.TryGetValue(cell, out var list))
                cellBuffer.AddRange(list);
        }

        return cellBuffer.Any(t => rect.IntersectsWith(t));
    }

    private static IEnumerable<(int, int)> GetCells(Rectangle r)
    {
        var x1 = FloorDiv(r.Left, CellSize);
        var x2 = FloorDiv(r.Right - 1, CellSize);
        var y1 = FloorDiv(r.Top, CellSize);
        var y2 = FloorDiv(r.Bottom - 1, CellSize);

        for (var x = x1; x <= x2; x++)
        for (var y = y1; y <= y2; y++)
            yield return (x, y);
    }

    private static int FloorDiv(int a, int b)
    {
        var div = a / b;
        if (a < 0 && a % b != 0) div--;
        return div;
    }

    private static int HashRect(Rectangle r)
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + r.X;
            hash = hash * 31 + r.Y;
            hash = hash * 31 + r.Width;
            hash = hash * 31 + r.Height;
            return hash;
        }
    }

    private double DistanceToCenterInt(Rectangle r)
    {
        var dx = r.X + (r.Width >> 1) - centerX;
        var dy = r.Y + (r.Height >> 1) - centerY;
        return dx * dx + dy * dy;
    }

    private static Rectangle CreateRectangleByCenter(Point center, Size size) =>
        new(center.X - size.Width / 2, center.Y - size.Height / 2, size.Width, size.Height);

    private Rectangle FindBySpiralFallback(Size size)
    {
        for (var i = 0; i < 20000; i++)
        {
            var p = spiral.GetNextPoint();
            var r = CreateRectangleByCenter(p, size);
            if (!IntersectsGrid(r))
                return r;
        }

        return new Rectangle(centerX + 50000, centerY + 50000, size.Width, size.Height);
    }

    private IEnumerable<Rectangle> GetSpiralCandidates(Size size, int count = 10)
    {
        var found = 0;
        while (found < count)
        {
            var p = spiral.GetNextPoint();
            var rect = CreateRectangleByCenter(p, size);
            yield return rect;
            found++;
        }
    }
}
