using System.Drawing;
using System.Runtime.Versioning;

namespace TagsCloudVisualization;

[SupportedOSPlatform("windows")]

public static class CloudVisualizer
{
    public static void GenerateWordsCloud(string fileName, List<Size> rectSizes)
    {
        var center = new Point(0, 0);
        var layouter = new CircularCloudLayouter(center);

        foreach (var size in rectSizes)
        {
            layouter.PutNextRectangle(size);
        }

        SaveLayoutImage(layouter.PlacedRectangles, fileName);
    }
    
    private static void SaveLayoutImage(List<Rectangle> rectangles, string fileName, int padding = 50)
    {
        if (rectangles.Count == 0)
            return;

        var minX = rectangles.Min(r => r.Left);
        var maxX = rectangles.Max(r => r.Right);
        var minY = rectangles.Min(r => r.Top);
        var maxY = rectangles.Max(r => r.Bottom);

        var width = (maxX - minX) + padding * 2;
        var height = (maxY - minY) + padding * 2;

        using var bitmap = new Bitmap(width, height);
        using var g = Graphics.FromImage(bitmap);

        g.Clear(Color.Black);

        var random = new Random();

        foreach (var rect in rectangles)
        {
            var shifted = rect with { X = rect.Left - minX + padding, Y = rect.Top - minY + padding };
            if (rect == rectangles[0])
            {
                g.FillRectangle(Brushes.Red, shifted);
                continue;
            }
            var color = Color.FromArgb(
                random.Next(60, 93),
                0,
                random.Next(164, 255));

            using var brush = new SolidBrush(color);
            g.FillRectangle(brush, shifted);
            g.DrawRectangle(Pens.Black, shifted);
        }

        bitmap.Save(fileName);
    }
}