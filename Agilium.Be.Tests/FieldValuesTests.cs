using System.Text.Json;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Eng.Agilium.Be.Tests;

public class FieldValuesTests(ApiFixture fixture) : ApiTestBase(fixture)
{
  private const int Task = (int)ItemType.Task;

  private async Task<(TestApi user, int project, int task)> ArrangeAsync()
  {
    var user = await Api.RegisterAndLoginAsync();
    var project = await user.CreateProjectAsync();
    return (user, project, await user.CreateItemAsync(project, "T", Task));
  }

  /// <summary>Adds a project-template attribute of the given type and returns its id.</summary>
  private async Task<int> AddAttributeAsync(TestApi user, int project, TemplateItemType type, string? regex = null)
  {
    var key = $"attr-{Guid.NewGuid():N}";
    await Factory.WithDbAsync(async db =>
    {
      var section = await db
        .TemplateSections.Where(s => s.Column.Table.Template.ProjectId == project && s.Column.Table.Template.Type == ItemType.Task)
        .OrderBy(s => s.Id)
        .FirstAsync();
      db.TemplateItems.Add(
        new TemplateItem
        {
          TemplateSectionId = section.Id,
          Key = key,
          Title = key,
          Type = type,
          ValidatingRegex = regex,
          OrderIndex = 1000,
        }
      );
      await db.SaveChangesAsync();
    });
    return await Factory.WithDbAsync(db => db.TemplateItems.Where(i => i.Key == key).Select(i => i.Id).SingleAsync());
  }

  private static Task<ApiResponse> SetAsync(TestApi user, int task, int templateItemId, string? value) =>
    user.Put($"projects/items/{task}/fields", new { values = new[] { new { templateItemId, value } } });

  [Theory]
  [InlineData(TemplateItemType.Integer, "42", true)]
  [InlineData(TemplateItemType.Integer, "4.2", false)]
  [InlineData(TemplateItemType.Integer, "abc", false)]
  [InlineData(TemplateItemType.Decimal, "4.5", true)]
  [InlineData(TemplateItemType.Decimal, "4,5", true)] // decimal comma is normalized
  [InlineData(TemplateItemType.Decimal, "x", false)]
  [InlineData(TemplateItemType.Boolean, "true", true)]
  [InlineData(TemplateItemType.Boolean, "false", true)]
  [InlineData(TemplateItemType.Boolean, "maybe", false)]
  [InlineData(TemplateItemType.SingleLineText, "hello", true)]
  [InlineData(TemplateItemType.MultiLineText, "a\nb", true)]
  [InlineData(TemplateItemType.Comments, "plain text", false)] // comments go through their own endpoints
  [InlineData(TemplateItemType.LabelOnly, "anything", false)]
  public async Task Set_ValidatesTheValueByAttributeType(TemplateItemType type, string value, bool valid)
  {
    var (user, project, task) = await ArrangeAsync();
    var attribute = await AddAttributeAsync(user, project, type);

    var response = await SetAsync(user, task, attribute, value);

    response.Code.ShouldBe(valid ? 204 : 400);
  }

  [Fact]
  public async Task Set_DecimalComma_IsStoredWithADot()
  {
    var (user, project, task) = await ArrangeAsync();
    var attribute = await AddAttributeAsync(user, project, TemplateItemType.Decimal);

    (await SetAsync(user, task, attribute, "4,5")).Code.ShouldBe(204);

    (await Factory.WithDbAsync(db => db.ItemFieldValues.Where(v => v.TemplateItemId == attribute).Select(v => v.Value).SingleAsync()))
      .ShouldBe("4.5");
  }

  [Fact]
  public async Task Set_ValidatingRegex_IsEnforced()
  {
    var (user, project, task) = await ArrangeAsync();
    var attribute = await AddAttributeAsync(user, project, TemplateItemType.SingleLineText, "^[A-Z]{3}$");

    (await SetAsync(user, task, attribute, "abc")).Code.ShouldBe(400);
    (await SetAsync(user, task, attribute, "ABC")).Code.ShouldBe(204);
  }

  [Fact]
  public async Task Set_TemplateItemOfAnotherTemplate_Returns400()
  {
    var (user, project, task) = await ArrangeAsync();
    var otherProject = await user.CreateProjectAsync();
    var foreign = await AddAttributeAsync(user, otherProject, TemplateItemType.SingleLineText);

    (await SetAsync(user, task, foreign, "x")).Code.ShouldBe(400);
  }

  [Fact]
  public async Task Get_ReturnsTheStoredValue()
  {
    var (user, project, task) = await ArrangeAsync();
    var attribute = await AddAttributeAsync(user, project, TemplateItemType.SingleLineText);
    await SetAsync(user, task, attribute, "stored");

    var response = await user.Get($"projects/items/{task}/fields");

    var values = response
      .Json.GetProperty("tables")
      .EnumerateArray()
      .SelectMany(t => t.GetProperty("columns").EnumerateArray())
      .SelectMany(c => c.GetProperty("sections").EnumerateArray())
      .SelectMany(s => s.GetProperty("items").EnumerateArray())
      .Single(a => a.GetProperty("templateItemId").GetInt32() == attribute);
    values.GetProperty("value").GetString().ShouldBe("stored");
  }
}
