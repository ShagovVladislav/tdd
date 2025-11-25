using System.Drawing;

namespace TagsCloudVisualization;

public class Spiral
{
    private readonly Point center;
    private double angle;
    private readonly double angleStep;
    private readonly double radiusStep;

    public Spiral(Point center, double angleStep = 0.1, double radiusStep = 0.5)
    {
        this.center = center;
        this.angleStep = angleStep;
        this.radiusStep = radiusStep;
        angle = 0;
    }

    public Point GetNextPoint()
    {
        var radius = radiusStep * angle;
        var x = center.X + (int)(radius * Math.Cos(angle));
        var y = center.Y + (int)(radius * Math.Sin(angle));

        angle += angleStep;
        return new Point(x, y);
    }
}
