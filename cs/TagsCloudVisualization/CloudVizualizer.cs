using System.Drawing;
using System.Runtime.Versioning;

namespace TagsCloudVisualization;

[SupportedOSPlatform("windows")]
public static class CloudVisualizer
{
    public static void GenerateRectanglesCloud(string fileName, List<Size> rectSizes)
    {
        var center = new Point(0, 0);
        var layouter = new CircularCloudLayouter(center);

        foreach (var size in rectSizes)
        {
            layouter.PutNextRectangle(size);
        }

        SaveLayoutImage(layouter.PlacedRectangles, fileName);
    }
    
    private static void SaveLayoutImage(IReadOnlyList<Rectangle> rectangles, string fileName, int padding = 50)
    {
        switch (rectangles.Count)
        {
            case 0:
                Console.WriteLine("There are no rectangles that can be used");
                return;
            case >= 100000:
                Console.WriteLine("There are too many rectangles");
                return;
        }

        var minX = rectangles.Min(r => r.Left);
        var maxX = rectangles.Max(r => r.Right);
        var minY = rectangles.Min(r => r.Top);
        var maxY = rectangles.Max(r => r.Bottom);

        var width = (maxX - minX) + padding * 2;
        var height = (maxY - minY) + padding * 2;

        using var bitmap = new Bitmap(width, height);
        using var g = Graphics.FromImage(bitmap);

        g.Clear(Color.Black);
        var colors = GenerateColorPalette(rectangles.Count - 1);
        var colorIndex = 0;
        foreach (var rect in rectangles)
        {
            var shifted = rect with { X = rect.Left - minX + padding, Y = rect.Top - minY + padding };
            if (rect == rectangles[0])
            {
                g.FillRectangle(Brushes.Red, shifted);
                continue;
            }
            
            using var brush = new SolidBrush(colors[colorIndex++]);
            g.FillRectangle(brush, shifted);
            g.DrawRectangle(Pens.Black, shifted);
        }
        bitmap.Save(fileName);
    }
    
    private static Color[] GenerateColorPalette(int count)
    {
        var colors = new Color[count];
        for (var i = 0; i < count; i++)
        {
            colors[i] = Color.FromArgb(
                Random.Shared.Next(60, 93),
                0,
                Random.Shared.Next(164, 255));
        }
        return colors;
    }
}