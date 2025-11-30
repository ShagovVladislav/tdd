using System.Drawing;
using System.Runtime.Versioning;
using TagsCloudVisualization;

namespace TagsCloudVizualizationUser;
[SupportedOSPlatform("windows")]
class Program
{
    
public static void Main()
{
    var outputDir = Path.Combine("visualizer_tests");
    Directory.CreateDirectory(outputDir);
    var file1 = Path.Combine(outputDir, "example1.png");
    var file2 = Path.Combine(outputDir, "example2.png");
    var file3 = Path.Combine(outputDir, "example3.png");
    var sizes1 = Enumerable
        .Range(1, 500)
        .Select(i => new Size(i % 40 + 10, i % 30 + 10))
        .ToList();
    var sizes2 = Enumerable
        .Range(1, 100)
        .Select(i => new Size(i % 60 + 10, i % 20 + 10))
        .ToList();
    var sizes3 = Enumerable
        .Range(1, 50)
        .Select(i => new Size(i % 40 + 10, i % 70 + 10))
        .ToList();
    CloudVisualizer.GenerateRectanglesCloud(file1, sizes1);
    CloudVisualizer.GenerateRectanglesCloud(file2, sizes2);
    CloudVisualizer.GenerateRectanglesCloud(file3, sizes3);
}
}
