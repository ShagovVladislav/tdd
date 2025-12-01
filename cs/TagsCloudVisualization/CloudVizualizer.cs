using System.Drawing;
using System.Runtime.Versioning;

namespace TagsCloudVisualization;

[SupportedOSPlatform("windows")]
public static class CloudVisualizer
{
    public static void GenerateRectanglesCloud(
        string fileName,
        List<Size> rectSizes,
        Point? center = null,
        Size? imageSize = null,
        Color? backgroundColor = null,
        Color? baseRectangleColor = null,
        Color? rectangleBorderColor = null,
        string? outputDirectory = null)
    {
        try
        {
            center ??= new Point(0, 0);
            imageSize ??= new Size(0, 0); 
            backgroundColor ??= Color.Black;
            baseRectangleColor ??= Color.Blue;
            rectangleBorderColor ??= Color.Black;
            outputDirectory ??= Environment.CurrentDirectory;
            
            ValidateInputs(fileName, outputDirectory, rectSizes);
            
            Directory.CreateDirectory(outputDirectory);
            
            var fullPath = Path.Combine(outputDirectory, fileName);
            
            Console.WriteLine($"Creating cloud at: {fullPath}");
            
            var layouter = new CircularCloudLayouter(center.Value);

            foreach (var size in rectSizes)
                layouter.PutNextRectangle(size);

            using var pen = new Pen(rectangleBorderColor.Value);
            SaveLayoutImage(layouter.PlacedRectangles, fullPath, imageSize.Value, 
                backgroundColor.Value, baseRectangleColor.Value, pen);
            
            Console.WriteLine($"File successfully saved to: {fullPath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error generating rectangles cloud: {ex.Message}");
            throw;
        }
    }

    private static void ValidateInputs(string fileName, string outputDirectory, List<Size> rectSizes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
            
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
        {
            throw new ArgumentException("File extension is required (.png, .jpg)", nameof(fileName));
        }
        else
        {
            var validExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
            if (!validExtensions.Contains(extension.ToLowerInvariant()))
                throw new ArgumentException($"Invalid file extension. Use: {string.Join(", ", validExtensions)}", 
                    nameof(fileName));
        }
        
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be null or empty", nameof(outputDirectory));
            
        try
        {
            var fullPath = Path.GetFullPath(outputDirectory);
            Console.WriteLine($"Output directory resolved to: {fullPath}");
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid output directory: {ex.Message}", nameof(outputDirectory));
        }
        
        switch (rectSizes)
        {
            case null:
                throw new ArgumentNullException(nameof(rectSizes), "Rectangle sizes cannot be null");
            case { Count: 0 }:
                throw new ArgumentException("There are no rectangles that can be used", nameof(rectSizes));
            case { Count: > 10000 }:
                throw new ArgumentException("There are too many rectangles. Maximum is 10000", nameof(rectSizes));
        }
    }

    private static void SaveLayoutImage(
        IReadOnlyList<Rectangle> rectangles,
        string fileName,
        Size imageSize,
        Color backgroundColor,
        Color baseRectangleColor,
        Pen? pen = null,
        int padding = 50)
    {
        try
        {
            var directory = Path.GetDirectoryName(fileName);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Console.WriteLine($"Creating directory: {directory}");
                Directory.CreateDirectory(directory);
            }
            
            pen ??= Pens.Black;

            var minX = rectangles.Min(r => r.Left);
            var maxX = rectangles.Max(r => r.Right);
            var minY = rectangles.Min(r => r.Top);
            var maxY = rectangles.Max(r => r.Bottom);

            var width = imageSize.Width > 0 ? imageSize.Width : (maxX - minX) + padding * 2;
            var height = imageSize.Height > 0 ? imageSize.Height : (maxY - minY) + padding * 2;
            
            if (width <= 0 || height <= 0)
                throw new InvalidOperationException($"Invalid image dimensions: {width}x{height}");
                
            if (width > 10000 || height > 10000)
                throw new InvalidOperationException($"Image dimensions too large: {width}x{height}");

            using var bitmap = new Bitmap(width, height);
            using var g = Graphics.FromImage(bitmap);

            g.Clear(backgroundColor);

            var colors = PaletteGenerator.GenerateColorPalette(rectangles.Count - 1, baseRectangleColor);
            var colorIndex = 0;

            foreach (var rect in rectangles)
            {
                var shifted = rect with
                {
                    X = rect.Left - minX + padding,
                    Y = rect.Top - minY + padding
                };

                if (rect == rectangles[0])
                {
                    using var centerBrush = new SolidBrush(baseRectangleColor);
                    g.FillRectangle(centerBrush, shifted);
                    continue;
                }

                using var brush = new SolidBrush(colors[colorIndex++]);
                g.FillRectangle(brush, shifted);
                g.DrawRectangle(pen, shifted);
            }

            Console.WriteLine($"Saving image to: {fileName}");
            bitmap.Save(fileName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save image '{fileName}': {ex.Message}", ex);
        }
    }
}