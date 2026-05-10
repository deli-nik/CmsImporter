using CmsImporter.Application.Pipeline;
using CmsImporter.Core.Entities;

using NSubstitute;

namespace CmsImporter.Application.Tests.Pipeline;

[TestFixture]
public sealed class TransformStageTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 5, 10, 12, 0, 0, TimeSpan.Zero);

    private TransformStage _stage = null!;

    [SetUp]
    public void SetUp()
    {
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(FixedNow);
        _stage = new TransformStage(time);
    }

    [Test]
    public void Execute_MapsAllRequiredFields()
    {
        var raw = TestSamples.NewRaw(externalId: "id-42", title: "Welcome", type: "Page");

        var result = _stage.Execute(raw);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExternalId, Is.EqualTo("id-42"));
            Assert.That(result.Title, Is.EqualTo("Welcome"));
            Assert.That(result.Type, Is.EqualTo(ContentType.Page));
            Assert.That(result.ImportedAt, Is.EqualTo(FixedNow));
            Assert.That(result.Body.Raw, Is.EqualTo("body"));
        });
    }

    [TestCase("Hello World", ExpectedResult = "hello-world")]
    [TestCase("  Mixed  Case  ", ExpectedResult = "mixed-case")]
    [TestCase("special@chars!?", ExpectedResult = "special-chars")]
    [TestCase("\t\n", ExpectedResult = "untitled")]
    [TestCase("---", ExpectedResult = "untitled")]
    [TestCase("Q1 2024 — launch!", ExpectedResult = "q1-2024-launch")]
    public string Execute_GeneratesSlugFromTitle_WhenSlugMissing(string title) =>
        _stage.Execute(TestSamples.NewRaw(title: title, slug: null)).Slug;

    [Test]
    public void Execute_PreservesProvidedSlug()
    {
        var raw = TestSamples.NewRaw(title: "Hello World", slug: "my-explicit-slug");
        Assert.That(_stage.Execute(raw).Slug, Is.EqualTo("my-explicit-slug"));
    }

    [TestCase("Page", ContentType.Page)]
    [TestCase("page", ContentType.Page)]
    [TestCase("PAGE", ContentType.Page)]
    [TestCase("Article", ContentType.Article)]
    [TestCase("post", ContentType.Article)]
    [TestCase("Media", ContentType.Media)]
    [TestCase("asset", ContentType.Media)]
    [TestCase("WidgetX", ContentType.Unknown)]
    [TestCase("", ContentType.Unknown)]
    public void Execute_ParsesType(string raw, ContentType expected)
    {
        var content = _stage.Execute(TestSamples.NewRaw(type: raw, title: "T"));
        Assert.That(content.Type, Is.EqualTo(expected));
    }

    [Test]
    public void Execute_CopiesMetadata_IntoNewDictionary()
    {
        var input = new Dictionary<string, string> { ["category"] = "news", ["tags"] = "a,b" };
        var raw = TestSamples.NewRaw(metadata: input);

        var result = _stage.Execute(raw);

        Assert.Multiple(() =>
        {
            Assert.That(result.Metadata, Has.Count.EqualTo(2));
            Assert.That(result.Metadata["category"], Is.EqualTo("news"));
            Assert.That(result.Metadata, Is.Not.SameAs(input), "should be defensively copied");
        });
    }

    [Test]
    public void Execute_NullMetadata_BecomesEmptyDictionary()
    {
        var result = _stage.Execute(TestSamples.NewRaw(metadata: null));
        Assert.That(result.Metadata, Is.Empty);
    }

    [Test]
    public void Execute_ThrowsOnNull() =>
        Assert.That(() => _stage.Execute(null!), Throws.ArgumentNullException);
}
