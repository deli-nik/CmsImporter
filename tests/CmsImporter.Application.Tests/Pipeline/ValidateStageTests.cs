using CmsImporter.Application.Pipeline;
using CmsImporter.Core.Entities;
using CmsImporter.Core.ValueObjects;

namespace CmsImporter.Application.Tests.Pipeline;

[TestFixture]
public sealed class ValidateStageTests
{
    private readonly ValidateStage _stage = new();

    [Test]
    public void Execute_ValidItem_Passes()
    {
        var item = NewValidItem();

        var result = _stage.Execute(item);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public void Execute_MissingExternalId_Fails()
    {
        var item = NewValidItem();
        item.ExternalId = "";

        var result = _stage.Execute(item);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Some.Contains("ExternalId"));
    }

    [Test]
    public void Execute_MissingSourceSystem_Fails()
    {
        var item = NewValidItem();
        item.SourceSystem = "   ";

        var result = _stage.Execute(item);

        Assert.That(result.Errors, Has.Some.Contains("SourceSystem"));
    }

    [Test]
    public void Execute_MissingTitle_Fails()
    {
        var item = NewValidItem();
        item.Title = "";

        var result = _stage.Execute(item);

        Assert.That(result.Errors, Has.Some.Contains("Title"));
    }

    [Test]
    public void Execute_UnknownType_Fails()
    {
        var item = NewValidItem();
        item.Type = ContentType.Unknown;

        var result = _stage.Execute(item);

        Assert.That(result.Errors, Has.Some.Contains("Type"));
    }

    [Test]
    public void Execute_TitleTooLong_Fails()
    {
        var item = NewValidItem();
        item.Title = new string('x', 501);

        var result = _stage.Execute(item);

        Assert.That(result.Errors, Has.Some.Contains("500 characters"));
    }

    [Test]
    public void Execute_AccumulatesMultipleErrors()
    {
        var item = NewValidItem();
        item.ExternalId = "";
        item.Title = "";
        item.Type = ContentType.Unknown;

        var result = _stage.Execute(item);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Count.EqualTo(3));
    }

    [Test]
    public void Execute_ThrowsOnNull() =>
        Assert.That(() => _stage.Execute(null!), Throws.ArgumentNullException);

    private static ContentItem NewValidItem() => new()
    {
        ExternalId = "ext-1",
        SourceSystem = "test",
        Type = ContentType.Page,
        Title = "Title",
        Slug = "title",
        Body = ContentBody.Empty(),
        Metadata = new Dictionary<string, string>(),
        ImportedAt = DateTimeOffset.UtcNow,
    };
}
