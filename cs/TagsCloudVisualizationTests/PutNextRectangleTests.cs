using System.Drawing;
using System.Runtime.Versioning;
using FluentAssertions;
using NUnit.Framework.Interfaces;
using TagsCloudVisualization;
using static TagsCloudVisualization.CloudVisualizer;
namespace TagsCloudVisualizationTests;

[SupportedOSPlatform("windows")]

[TestFixture]
public class PutNextRectangleTests
{
    private List<Size> generatedRectSizes;

    [SetUp]
    public void SetUp()
    {
        generatedRectSizes = [];
    }

    [Test]
    public void PutNextRectangle_FirstRectangle_ShouldBeCentered()
    {
        var center = new Point(100, 100);
        var size = new Size(20, 20);
        var layouter = new CircularCloudLayouter(center);

        var rect = layouter.PutNextRectangle(size);
        generatedRectSizes.Add(size);

        rect.X.Should().Be(center.X - size.Width / 2);
        rect.Y.Should().Be(center.Y - size.Height / 2);
    }

    [Test]
    public void PutNextRectangle_AllRectanglesShouldHaveUniquePositions()
    {
        var layouter = new CircularCloudLayouter(new Point(0, 0));
        var size = new Size(30, 20);
        for (var i = 0; i < 100; i++)
        {
            layouter.PutNextRectangle(size);
            generatedRectSizes.Add(size);
        }

        layouter.PlacedRectangles
            .Select(r => (r.X, r.Y))
            .Should()
            .OnlyHaveUniqueItems();
    }
    
    [Test]
    public void PutNextRectangle_ShouldNotCrossCenter()
    {
        var center = new Point(100, 100);
        var layouter = new CircularCloudLayouter(center);
        var size1 = new Size(150, 150);
        var size2 = new Size(30, 30);
        
        layouter.PutNextRectangle(size1);
        generatedRectSizes.Add(size1);
        var rect = layouter.PutNextRectangle(size2);
        generatedRectSizes.Add(size2);
        var centerPoint = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);

        centerPoint.Should().NotBe(center);
    }

    [Test]
    public void PutNextRectangle_ShouldFindFreePlace_WhenCenterIsOccupied()
    {
        var center = new Point(100, 100);
        var layouter = new CircularCloudLayouter(center);
        var size1 = new Size(200, 200);
        var size2 = new Size(20, 20);
        
        layouter.PutNextRectangle(size1);
        generatedRectSizes.Add(size1);
        var rect2 = layouter.PutNextRectangle(size2);
        generatedRectSizes.Add(size2);
        
        rect2.IntersectsWith(layouter.PlacedRectangles[0])
            .Should()
            .BeFalse();
    }

    [Test]
    public void PutNextRectangle_ShouldShiftRectangleCloserToCenter_ByX()
    {
        var center = new Point(100, 100);
        var layouter = new CircularCloudLayouter(center);
        var size1 = new Size(200, 50);
        var size2 = new Size(30, 30);

        layouter.PutNextRectangle(size1);
        generatedRectSizes.Add(size1);
        var rect = layouter.PutNextRectangle(size2);
        generatedRectSizes.Add(size2);
        
        rect.X.Should().BeLessThan(center.X);
    }

    [Test]
    public void PutNextRectangle_ShouldShiftRectangleCloserToCenter_ByY()
    {
        var center = new Point(100, 100);
        var layouter = new CircularCloudLayouter(center);
        var size1 = new Size(200, 200);
        var size2 = new Size(20, 20);
        
        layouter.PutNextRectangle(new Size(200, 200));
        generatedRectSizes.Add(size1);
        var rect = layouter.PutNextRectangle(new Size(20, 20));
        generatedRectSizes.Add(size2);

        rect.Y.Should().BeGreaterThan(center.Y);
    }

    [Test]
    public void PutNextRectangle_ShouldThrow_OnNegativeSize()
    {
        var layouter = new CircularCloudLayouter(new Point(0, 0));
        var size = new Size(-10, 20);
        
        var action = () => layouter.PutNextRectangle(size);
        generatedRectSizes.Add(size);
        action.Should().Throw<ArgumentException>();
    }
    
    [TearDown]
    public void TearDown()
    {
        var context = TestContext.CurrentContext;

        if (context.Result.Outcome.Status != TestStatus.Failed)
            return;

        try
        {
            var dir = Path.Combine(context.WorkDirectory, "failures");
            Directory.CreateDirectory(dir);

            var fileName = $"{context.Test.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";

            var fullPath = Path.Combine(dir, fileName);

            GenerateWordsCloud(fullPath, generatedRectSizes);

            TestContext.Out.WriteLine($"Tag cloud visualization saved to file {fullPath}");
        }
        catch (Exception e)
        {
            TestContext.Out.WriteLine("Failed to save visualization: " + e);
        }
    }
}
