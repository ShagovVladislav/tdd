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
    public void GenerateWordsCloud_ShouldCreateFile_WhenSizesProvided()
    {
        var file = Path.Combine(outputDir, "cloud.png");
        var sizes = new List<Size>
        {
            new(50, 30),
            new(20, 20),
            new(10, 40)
        };

        CloudVisualizer.GenerateWordsCloud(file, sizes);

        File.Exists(file).Should().BeTrue("visualizer must create output file");
    }

    [Test]
    public void GenerateWordsCloud_ShouldNotThrow_WhenGivenEmptyList()
    {
        var file = Path.Combine(outputDir, "empty.png");

        var act = () => CloudVisualizer.GenerateWordsCloud(file, []);

        act.Should().NotThrow("empty list shouldn't cause failures");
        File.Exists(file).Should().BeFalse("empty input should not produce image");
    }

    [Test]
    public void GenerateWordsCloud_ShouldProduceNonEmptyImage()
    {
        var file = Path.Combine(outputDir, "nonempty.png");
        var sizes = new List<Size> { new(100, 50), new(30, 30) };

        CloudVisualizer.GenerateWordsCloud(file, sizes);

        var fileInfo = new FileInfo(file);
        
        fileInfo.Exists.Should().BeTrue();
        fileInfo.Length.Should().BeGreaterThan(1000, "image should not be empty");
    }

    [Test]
    public void GenerateWordsCloud_ShouldProduceValidPng()
    {
        var file = Path.Combine(outputDir, "valid.png");
        var sizes = new List<Size> { new(80, 40) };

        CloudVisualizer.GenerateWordsCloud(file, sizes);

        using var bmp = new Bitmap(file);

        bmp.Width.Should().BeGreaterThan(0);
        bmp.Height.Should().BeGreaterThan(0);
    }

    [Test]
    public void GenerateWordsCloud_ShouldUseAllSizes()
    {
        var file = Path.Combine(outputDir, "count_check.png");
        var sizes = new List<Size>
        {
            new(10,10),
            new(20,20),
            new(30,30)
        };

        CloudVisualizer.GenerateWordsCloud(file, sizes);

        var layouter = new CircularCloudLayouter(new Point(0, 0));
        foreach (var size in sizes)
            layouter.PutNextRectangle(size);

        layouter.PlacedRectangles.Count.Should().Be(sizes.Count);
    }

    [Test]
    public void GenerateWordsCloud_ShouldHandleLargeAmountOfRects()
    {
        var file = Path.Combine(outputDir, "many.png");

        var sizes = Enumerable
            .Range(1, 200)
            .Select(i => new Size(i % 40 + 10, i % 30 + 10))
            .ToList();

        var act = () => CloudVisualizer.GenerateWordsCloud(file, sizes);

        act.Should().NotThrow();
        File.Exists(file).Should().BeTrue();
    }

    [Test, Explicit]
    public void VizualizeExamples()
    {
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
        CloudVisualizer.GenerateWordsCloud(file1, sizes1);
        CloudVisualizer.GenerateWordsCloud(file2, sizes2);
        CloudVisualizer.GenerateWordsCloud(file3, sizes3);
    }
}
