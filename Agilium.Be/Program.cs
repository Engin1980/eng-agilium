/* Initialization */

using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;
using Eng.Agilium.Be;
using Eng.Agilium.Be.Features;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var bi = new BuildInitializer(builder);
bi.Initialize();

var app = builder.Build();
var ai = new AppInitializer(app, bi.AppSettings!);
ai.LogPreLogs(bi.Logs);
ai.Initialize();

app.MapGet("/", () => "Eng-LF-SZZ-BE is running!");

app.Run();

class BuildInitializer(WebApplicationBuilder builder)
{
  private readonly WebApplicationBuilder builder = builder;

  public AppSettings? AppSettings { get; private set; }

  public List<(LogLevel, string)> Logs { get; } = [];

  public void Initialize()
  {
    DotNetEnv.Env.Load();
    ConfigureLogging();
    ConfigureConfigurations();
    EnsureAppSettingsNotNull();
    ConfigureCors();
    ConfigureDatabase();
    ConfigureServices();
    ConfigureAuthentization();
    ConfigureHandlersAndEndpoints();
    ConfigureSwagger();
  }

  private void ConfigureSwagger()
  {
    EnsureAppSettingsNotNull();

    if (AppSettings!.UseSwagger == false)
      return;

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
      this.Logs.Add((LogLevel.Information, "Enabling Swagger"));
      options.CustomSchemaIds(type => type.FullName);
      options.OrderActionsBy(apiDesc => apiDesc.RelativePath);
    });
  }

  private void ConfigureServices()
  {
    // exceptions
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // rate limiting: configure a global partitioned sliding-window limiter (per-client by IP/X-Forwarded-For)
    builder.Services.AddRateLimiter(options =>
    {
      var sett = AppSettings!.Security.RequestLimiter;

      options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
      {
        var key =
          httpContext.Connection.RemoteIpAddress?.ToString()
          ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
          ?? "global";

        return RateLimitPartition.GetSlidingWindowLimiter(
          key,
          _ => new SlidingWindowRateLimiterOptions
          {
            Window = TimeSpan.FromSeconds(sett.WindowLength),
            SegmentsPerWindow = sett.SegmentsPerWindow,
            PermitLimit = sett.PermitLimit,
            QueueLimit = sett.QueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
          }
        );
      });
      options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
      options.OnRejected = async (ctx, ct) =>
      {
        ctx.HttpContext.Response.Headers.RetryAfter = $"{sett.WindowLength}";
        await ctx.HttpContext.Response.WriteAsync("Too many requests", ct);
      };
    });

    // app services
    builder.Services.AddSingleton<AppSettingsService>();
    // builder.Services.AddTransient<TurnstileService>();
    // builder.Services.AddTransient<SmtpService>();
    // builder.Services.AddTransient<SmartEmailService>();

    // builder.Services.AddTransient<TokenService>();
  }

  private void ConfigureHandlersAndEndpoints()
  {
    IServiceCollection services = builder.Services;
    var assembly = typeof(Program).Assembly;

    var handlerTypes = assembly
      .GetTypes()
      .Where(t => t.IsClass && !t.IsAbstract && typeof(IHandler).IsAssignableFrom(t))
      .ToList();
    handlerTypes.ForEach(q => services.AddTransient(q));

    var endpointTypes = assembly
      .GetTypes()
      .Where(t => t.IsClass && !t.IsAbstract && typeof(IEndpoint).IsAssignableFrom(t))
      .ToList();
    endpointTypes.ForEach(q => services.AddTransient(q));
  }

  private void EnsureAppSettingsNotNull()
  {
    if (this.AppSettings == null)
    {
      throw new InvalidOperationException("AppSettings should be configured, but is empty.");
    }
  }

  private void ConfigureAuthentization()
  {
    EnsureAppSettingsNotNull();

    this.Logs.Add(
      (LogLevel.Information, $"T-key: {AppSettings?.Security?.Turnstile?.SecretKey[^2..] ?? "-?-no-key-?-"}")
    );
    this.Logs.Add((LogLevel.Information, $"J-key: {AppSettings?.Security.Jwt.Key[^2..] ?? "-?-no-key-?-"}"));

    JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
    var jwtKey = this.AppSettings!.Security.Jwt.Key;
    var jwtIssuer = this.AppSettings!.Security.Jwt.Issuer;

    builder
      .Services.AddAuthentication(options =>
      {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
      })
      .AddJwtBearer(options =>
      {
        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuer = true,
          ValidateAudience = false,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer = jwtIssuer,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
          NameClaimType = "sub",
          RoleClaimType = "role",
        };
      });

    builder.Services.AddAuthorization();
  }

  private void ConfigureDatabase()
  {
    var connStringName = "DefaultConnection";
    string conn =
      builder.Configuration.GetConnectionString(connStringName)
      ?? "-?-no-connection-string-provided-?--?-no-connection-string-provided-?-";
    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(conn));
    string connShort = conn.Length > 15 ? (conn.Substring(0, 15) + "...") : conn;
    this.Logs.Add(
      (LogLevel.Information, $"Configured DbContext with connection string named '{connStringName}': {connShort}")
    );
  }

  private void ConfigureLogging()
  {
    Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
    builder.Host.UseSerilog();
  }

  private void ConfigureConfigurations()
  {
    this.AppSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();
    builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
  }

  private void ConfigureCors()
  {
    EnsureAppSettingsNotNull();

    string feUrl = this.AppSettings!.FrontendBaseUrl ?? "-?-no-frontend-url-provided-?-";
    this.builder.Services.AddCors(options =>
    {
      options.AddDefaultPolicy(policy =>
      {
        policy.WithOrigins(feUrl).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
      });
    });
    Logs.Add((LogLevel.Information, $"Configured CORS policy with frontend URL: {feUrl}"));
  }
}

class AppInitializer(WebApplication app, AppSettings appSettings)
{
  private readonly WebApplication app = app;
  private readonly AppSettings appSettings = appSettings;
  private readonly ILogger<AppInitializer> logger = app.Services.GetRequiredService<ILogger<AppInitializer>>();

  public void LogPreLogs(List<(LogLevel, string)> logs)
  {
    foreach (var (level, message) in logs)
    {
      logger.Log(level, "PRE: " + message);
    }
    logs.Clear();
  }

  public void Initialize()
  {
    app.UseSerilogRequestLogging();

    logger.LogInformation("Starting application...");

    logger.LogInformation("Initializing database...");
    InitializeDatabase();

    logger.LogInformation("Configuring middleware and endpoints...");
    app.UseExceptionHandler();

    app.UseCors();
    app.UseRateLimiter();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    MapEndpoints();

    if (appSettings?.UseSwagger == true)
    {
      app.UseSwagger();
      app.UseSwaggerUI();
    }

    logger.LogInformation("Application initialized successfully.");
  }

  private void MapEndpoints()
  {
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Mapping endpoints...");
    var assembly = typeof(Program).Assembly;
    var endpointTypes = assembly
      .GetTypes()
      .Where(t => t.IsClass && !t.IsAbstract && typeof(IEndpoint).IsAssignableFrom(t))
      .ToList();

    logger.LogInformation("Found {Count} endpoints to map.", endpointTypes.Count);

    foreach (var endpointType in endpointTypes)
    {
      logger.LogDebug("Mapping endpoint: {EndpointType}", endpointType.FullName);

      var instance =
        scope.ServiceProvider.GetRequiredService(endpointType) as IEndpoint
        ?? throw new InvalidOperationException($"Could not create instance of endpoint '{endpointType.FullName}'.");
      instance.Map(app);
    }
  }

  private void InitializeDatabase()
  {
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    db.SaveChanges();
  }
}
