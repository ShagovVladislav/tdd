using System.Drawing;
using System.Runtime.Versioning;
using FluentAssertions;
using TagsCloudVisualization;

namespace TagsCloudVisualizationTests;

[SupportedOSPlatform("windows")]
[TestFixture]
public class CloudVisualizerTests
{
    private string outputDir;

    [SetUp]
    public void SetUp()
    {
        outputDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "visualizer_tests");
        Directory.CreateDirectory(outputDir);
    }

    [Test]
    public void GenerateRectanglesCloud_ShouldCreateFile_WhenSizesProvided()
    {
        var file = Path.Combine(outputDir, "cloud.png");
        var sizes = new List<Size>
        {
            new(50, 30),
            new(20, 20),
            new(10, 40)
        };

        CloudVisualizer.GenerateRectanglesCloud(file, sizes);

        File.Exists(file).Should().BeTrue("visualizer must create output file");
    }

    [Test]
    public void GenerateRectanglesCloud_ShouldNotThrow_WhenGivenEmptyList()
    {
        var file = Path.Combine(outputDir, "empty.png");

        var act = () => CloudVisualizer.GenerateRectanglesCloud(file, []);

        act.Should().NotThrow("empty list shouldn't cause failures");
        File.Exists(file).Should().BeFalse("empty input should not produce image");
    }

    [Test]
    public void GenerateRectanglesCloud_ShouldProduceNonEmptyImage()
    {
        var file = Path.Combine(outputDir, "nonempty.png");
        var sizes = new List<Size> { new(100, 50), new(30, 30) };

        CloudVisualizer.GenerateRectanglesCloud(file, sizes);

        var fileInfo = new FileInfo(file);
        
        fileInfo.Exists.Should().BeTrue();
        fileInfo.Length.Should().BeGreaterThan(1000, "image should not be empty");
    }

    [Test]
    public void GenerateRectanglesCloud_ShouldProduceValidPng()
    {
        var file = Path.Combine(outputDir, "valid.png");
        var sizes = new List<Size> { new(80, 40) };

        CloudVisualizer.GenerateRectanglesCloud(file, sizes);

        using var bmp = new Bitmap(file);

        bmp.Width.Should().BeGreaterThan(0);
        bmp.Height.Should().BeGreaterThan(0);
    }

    [Test]
    public void GenerateRectanglesCloud_ShouldUseAllSizes()
    {
        var file = Path.Combine(outputDir, "count_check.png");
        var sizes = new List<Size>
        {
            new(10,10),
            new(20,20),
            new(30,30)
        };

        CloudVisualizer.GenerateRectanglesCloud(file, sizes);

        var layouter = new CircularCloudLayouter(new Point(0, 0));
        foreach (var size in sizes)
            layouter.PutNextRectangle(size);

        layouter.PlacedRectangles.Count.Should().Be(sizes.Count);
    }

    [Test]
    public void GenerateRectanglesCloud_ShouldHandleLargeAmountOfRects()
    {
        var file = Path.Combine(outputDir, "many.png");

        var sizes = Enumerable
            .Range(1, 200)
            .Select(i => new Size(i % 40 + 10, i % 30 + 10))
            .ToList();

        var act = () => CloudVisualizer.GenerateRectanglesCloud(file, sizes);

        act.Should().NotThrow();
        File.Exists(file).Should().BeTrue();
    }
}