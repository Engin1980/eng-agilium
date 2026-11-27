using System.Text.Json;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Eng.Agilium.Be.Tests;

public class TemplatesTests(ApiFixture fixture) : ApiTestBase(fixture)
{
  private const int Task = (int)ItemType.Task;

  private async Task<(TestApi user, int project)> ArrangeAsync()
  {
    var user = await Api.RegisterAndLoginAsync();
    return (user, await user.CreateProjectAsync());
  }

  /// <summary>Converts the GET result into a PUT body (ids included), so a test can edit it.</summary>
  private static List<Dictionary<string, object?>> ToCommandTables(JsonElement getResult) =>
    getResult
      .GetProperty("tables")
      .EnumerateArray()
      .Select(t => new Dictionary<string, object?>
      {
        ["id"] = t.GetProperty("id").GetInt32(),
        ["columns"] = t.GetProperty("columns")
          .EnumerateArray()
          .Select(c => new Dictionary<string, object?>
          {
            ["id"] = c.GetProperty("id").GetInt32(),
            ["width"] = c.GetProperty("width").GetInt32(),
            ["sections"] = c.GetProperty("sections")
              .EnumerateArray()
              .Select(s => new Dictionary<string, object?>
              {
                ["id"] = s.GetProperty("id").GetInt32(),
                ["title"] = s.GetProperty("title").GetString(),
                ["items"] = s.GetProperty("items")
                  .EnumerateArray()
                  .Select(a => new Dictionary<string, object?>
                  {
                    ["id"] = a.GetProperty("id").GetInt32(),
                    ["key"] = a.GetProperty("key").GetString(),
                    ["title"] = a.GetProperty("title").GetString(),
                    ["type"] = a.GetProperty("type").GetInt32(),
                    ["validatingRegex"] = a.TryGetProperty("validatingRegex", out var r) && r.ValueKind == JsonValueKind.String ? r.GetString() : null,
                  })
                  .ToList(),
              })
              .ToList(),
          })
          .ToList(),
      })
      .ToList();

  private static List<Dictionary<string, object?>> Columns(Dictionary<string, object?> table) =>
    (List<Dictionary<string, object?>>)table["columns"]!;

  private static List<Dictionary<string, object?>> Sections(Dictionary<string, object?> column) =>
    (List<Dictionary<string, object?>>)column["sections"]!;

  private static List<Dictionary<string, object?>> Items(Dictionary<string, object?> section) =>
    (List<Dictionary<string, object?>>)section["items"]!;

  /// <summary>The section holding the numeric attributes (several items, so one can be removed and still leave some).</summary>
  private static Dictionary<string, object?> NumericSection(List<Dictionary<string, object?>> tables) =>
    tables
      .SelectMany(Columns)
      .SelectMany(Sections)
      .First(s => Items(s).Any(a => (int)a["type"]! == (int)TemplateItemType.Integer) && Items(s).Count >= 2);

  [Fact]
  public async Task Get_ReturnsTheProjectCopyOfTheDefaultTemplate()
  {
    var (user, project) = await ArrangeAsync();
    var response = await user.Get($"projects/{project}/templates/{Task}");

    response.Code.ShouldBe(200);
    var projectTemplateId = await Factory.WithDbAsync(db =>
      db.Templates.Where(t => t.ProjectId == project && t.Type == ItemType.Task).Select(t => t.Id).SingleAsync()
    );
    response.Json.GetProperty("templateId").GetInt32().ShouldBe(projectTemplateId);
    response.Json.GetProperty("tables").GetArrayLength().ShouldBeGreaterThan(0);
  }

  [Fact]
  public async Task Update_AddsUpdatesAndDeletesElements_AndLeavesTheGlobalTemplateAlone()
  {
    var (user, project) = await ArrangeAsync();
    var globalItemsBefore = await Factory.WithDbAsync(db => db.TemplateItems.CountAsync(i => i.Section.Column.Table.Template.ProjectId == null));
    var tables = ToCommandTables((await user.Get($"projects/{project}/templates/{Task}")).Json);
    var section = NumericSection(tables);
    var original = Items(section).Count;

    Items(section).RemoveAt(0); // delete one
    Items(section)[0]["title"] = "Renamed"; // update one
    Items(section).Add(new Dictionary<string, object?> { ["key"] = "extra", ["title"] = "Extra", ["type"] = (int)TemplateItemType.SingleLineText }); // add one

    var put = await user.Put($"projects/{project}/templates/{Task}", new { tables });
    put.Code.ShouldBe(200);

    var after = ToCommandTables((await user.Get($"projects/{project}/templates/{Task}")).Json);
    var items = Items(NumericSection(after));
    items.Count.ShouldBe(original);
    items.Select(i => i["title"]).ShouldContain("Renamed");
    items.Select(i => i["key"]).ShouldContain("extra");

    (await Factory.WithDbAsync(db => db.TemplateItems.CountAsync(i => i.Section.Column.Table.Template.ProjectId == null)))
      .ShouldBe(globalItemsBefore);
  }

  [Fact]
  public async Task Update_DeletingAnAttribute_RemovesItsFieldValuesAndReportsTheCount()
  {
    var (user, project) = await ArrangeAsync();
    var task = await user.CreateItemAsync(project, "T", Task);
    var tables = ToCommandTables((await user.Get($"projects/{project}/templates/{Task}")).Json);
    var section = NumericSection(tables);
    var attribute = Items(section).First(a => (int)a["type"]! == (int)TemplateItemType.Integer);
    (await user.Put($"projects/items/{task}/fields", new { values = new[] { new { templateItemId = attribute["id"], value = "7" } } })).Code.ShouldBe(204);

    Items(section).Remove(attribute);
    var put = await user.Put($"projects/{project}/templates/{Task}", new { tables });

    put.Code.ShouldBe(200);
    put.Json.GetProperty("deletedFieldValues").GetInt32().ShouldBe(1);
  }

  [Fact]
  public async Task Update_DuplicateKey_Returns400()
  {
    var (user, project) = await ArrangeAsync();
    var tables = ToCommandTables((await user.Get($"projects/{project}/templates/{Task}")).Json);
    var items = Items(Sections(Columns(tables[0])[0])[0]);
    items.Add(new Dictionary<string, object?> { ["key"] = items[0]["key"], ["title"] = "Dup", ["type"] = (int)TemplateItemType.SingleLineText });

    (await user.Put($"projects/{project}/templates/{Task}", new { tables })).Code.ShouldBe(400);
  }

  [Fact]
  public async Task Update_ColumnWidthZeroOrNoColumns_Returns400()
  {
    var (user, project) = await ArrangeAsync();
    var tables = ToCommandTables((await user.Get($"projects/{project}/templates/{Task}")).Json);

    Columns(tables[0])[0]["width"] = 0;
    (await user.Put($"projects/{project}/templates/{Task}", new { tables })).Code.ShouldBe(400);

    Columns(tables[0])[0]["width"] = 4;
    Columns(tables[0]).Clear();
    (await user.Put($"projects/{project}/templates/{Task}", new { tables })).Code.ShouldBe(400);
  }

  [Fact]
  public async Task Update_InvalidRegex_Returns400()
  {
    var (user, project) = await ArrangeAsync();
    var tables = ToCommandTables((await user.Get($"projects/{project}/templates/{Task}")).Json);
    var attribute = Items(Sections(Columns(tables[0])[0])[0])[0];
    attribute["type"] = (int)TemplateItemType.SingleLineText;
    attribute["validatingRegex"] = "([unclosed";

    (await user.Put($"projects/{project}/templates/{Task}", new { tables })).Code.ShouldBe(400);
  }

  [Fact]
  public async Task Update_WidthsNotSummingToTwelve_AreAccepted()
  {
    var (user, project) = await ArrangeAsync();
    var tables = ToCommandTables((await user.Get($"projects/{project}/templates/{Task}")).Json);
    Columns(tables[0])[0]["width"] = 3;
    Columns(tables[0])[1]["width"] = 3;

    (await user.Put($"projects/{project}/templates/{Task}", new { tables })).Code.ShouldBe(200);
  }

  [Fact]
  public async Task Update_RemovingACommentsElement_DeletesItsComments()
  {
    var (user, project) = await ArrangeAsync();
    var task = await user.CreateItemAsync(project, "T", Task);
    var element = await Factory.WithDbAsync(db => db.TemplateItems
      .Where(i => i.Type == TemplateItemType.Comments && i.Section.Column.Table.Template.ProjectId == project && i.Section.Column.Table.Template.Type == ItemType.Task)
      .Select(i => i.Id).SingleAsync());
    (await user.Post($"projects/items/{task}/comments", new { templateItemId = element, text = "hello" })).Code.ShouldBe(201);

    var tables = ToCommandTables((await user.Get($"projects/{project}/templates/{Task}")).Json);
    foreach (var column in tables.SelectMany(Columns))
      foreach (var section in Sections(column))
        Items(section).RemoveAll(i => (int)i["type"]! == (int)TemplateItemType.Comments);

    (await user.Put($"projects/{project}/templates/{Task}", new { tables })).Code.ShouldBe(200);

    (await Factory.WithDbAsync(db => db.ItemComments.CountAsync(c => c.ItemId == task))).ShouldBe(0);
  }
}
