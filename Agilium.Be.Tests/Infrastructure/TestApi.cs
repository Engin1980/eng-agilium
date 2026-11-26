using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Eng.Agilium.Be.Tests.Infrastructure;

public sealed record ApiResponse(HttpStatusCode Status, string Body, string? ErrorKey, IReadOnlyList<string> SetCookies)
{
  public int Code => (int)Status;

  public T As<T>() =>
    JsonSerializer.Deserialize<T>(Body, TestApi.Json)
    ?? throw new InvalidOperationException($"Cannot read '{Body}' as {typeof(T).Name}.");

  public JsonElement Json => JsonDocument.Parse(Body).RootElement;

  /// <summary>The <c>id</c> of an <c>IdResult</c> body.</summary>
  public int Id => Json.GetProperty("id").GetInt32();
}

/// <summary>Thin client over the in-memory API; one instance per (anonymous or logged-in) caller.</summary>
public sealed class TestApi
{
  public const string DefaultPassword = "Password-123";

  public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
  private readonly ApiFactory factory;
  private readonly HttpClient client;

  public TestApi(ApiFactory factory, string? accessToken = null, string? refreshCookie = null)
  {
    this.factory = factory;
    client = factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
    AccessToken = accessToken;
    RefreshCookie = refreshCookie;
    if (accessToken != null)
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
  }

  public string? AccessToken { get; }
  public string? RefreshCookie { get; }
  public int UserId { get; init; }
  public string Email { get; init; } = "";

  /// <summary>Same API without credentials.</summary>
  public TestApi Anonymous() => new(factory);

  public Task<ApiResponse> Get(string path) => Send(HttpMethod.Get, path, null);

  public Task<ApiResponse> Post(string path, object? body = null) => Send(HttpMethod.Post, path, body ?? new { });

  public Task<ApiResponse> Put(string path, object? body) => Send(HttpMethod.Put, path, body);

  public Task<ApiResponse> Patch(string path, object? body) => Send(HttpMethod.Patch, path, body ?? new { });

  public Task<ApiResponse> Delete(string path) => Send(HttpMethod.Delete, path, null);

  public async Task<ApiResponse> Send(HttpMethod method, string path, object? body, string? cookie = null)
  {
    using var request = new HttpRequestMessage(method, "/api/v1/" + path.TrimStart('/'));
    if (body != null)
      request.Content = JsonContent.Create(body, options: Json);
    if (cookie != null)
      request.Headers.Add("Cookie", cookie);
    using var response = await client.SendAsync(request);
    var text = await response.Content.ReadAsStringAsync();
    string? errorKey = null;
    if (!response.IsSuccessStatusCode && text.StartsWith('{'))
    {
      using var doc = JsonDocument.Parse(text);
      if (doc.RootElement.TryGetProperty("errorKey", out var ek))
        errorKey = ek.GetString();
    }
    var cookies = response.Headers.TryGetValues("Set-Cookie", out var v) ? v.ToList() : [];
    return new ApiResponse(response.StatusCode, text, errorKey, cookies);
  }

  /// <summary>Extracts the <c>refreshToken=...</c> pair from a Set-Cookie header.</summary>
  public static string? RefreshCookieFrom(ApiResponse response) =>
    response
      .SetCookies.Select(c => c.Split(';')[0])
      .FirstOrDefault(c => c.StartsWith("refreshToken=", StringComparison.Ordinal) && c.Length > "refreshToken=".Length);

  /// <summary>Registers a user (and, once the e-mail verification exists, verifies it) without logging in.</summary>
  public async Task<int> RegisterAsync(
    string email,
    string password = DefaultPassword,
    string name = "Test",
    string surname = "User"
  )
  {
    var response = await Anonymous().Post("auth/users", new { email, name, surname, password });
    if (response.Code != 201)
      throw new InvalidOperationException($"Registration failed: {response.Code} {response.Body}");
    return response.Id;
  }

  public async Task<ApiResponse> LoginRawAsync(string email, string password = DefaultPassword) =>
    await Anonymous().Post("auth/login/admin", new { email, password, rememberMe = true, turnstileToken = "" });

  /// <summary>Registers a new user, logs in and returns a client carrying the access token.</summary>
  public async Task<TestApi> RegisterAndLoginAsync(string? email = null, string password = DefaultPassword)
  {
    email ??= $"user-{Guid.NewGuid():N}@example.com";
    var id = await RegisterAsync(email, password);
    var login = await LoginRawAsync(email, password);
    if (login.Code != 200)
      throw new InvalidOperationException($"Login failed: {login.Code} {login.Body}");
    return new TestApi(factory, login.As<string>(), RefreshCookieFrom(login)) { UserId = id, Email = email };
  }

  public async Task<int> CreateProjectAsync(string? title = null, string description = "desc")
  {
    var response = await Post("projects", new { title = title ?? $"Project {Guid.NewGuid():N}", description });
    if (response.Code != 201)
      throw new InvalidOperationException($"Project creation failed: {response.Code} {response.Body}");
    return response.Id;
  }

  public async Task<int> CreateItemAsync(int projectId, string title, int type, int? parentId = null)
  {
    var response = await Post("projects/items", new { title, type, projectId, parentId });
    if (response.Code != 201)
      throw new InvalidOperationException($"Item creation failed: {response.Code} {response.Body}");
    return response.Id;
  }

  public async Task<int> CreateSprintAsync(int projectId, string? title = null, DateTime? start = null, DateTime? end = null)
  {
    var response = await Post(
      "projects/sprints",
      new
      {
        projectId,
        title = title ?? $"Sprint {Guid.NewGuid():N}",
        startDateTime = start,
        endDateTime = end,
      }
    );
    if (response.Code != 201)
      throw new InvalidOperationException($"Sprint creation failed: {response.Code} {response.Body}");
    return response.Id;
  }
}
