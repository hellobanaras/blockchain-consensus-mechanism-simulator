using Consensus.Web.Components;
using Consensus.Web.Hubs;
using Consensus.Web.Services;
using Consensus.Web.Middleware;
using Consensus.Data;
using Consensus.Core.Interfaces;
using Consensus.Core.Services;
using Consensus.Core.Services.Payloads;
using Consensus.Core.Entities;
using Consensus.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.File;

// Configure Serilog logger before creating builder
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information) 
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ConsensusSimulator")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/consensus-simulator-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext} ({RequestId}): {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog for logging
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        // Configure for container environment
        options.DetailedErrors = builder.Environment.IsDevelopment();
        options.DisconnectedCircuitMaxRetained = 100;
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
        options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
        options.MaxBufferedUnacknowledgedRenderBatches = 10;
    });

// Configure antiforgery services with container-friendly settings
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__RequestVerificationToken";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow non-HTTPS in container environment
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Path = "/";
});

// Add API controllers
builder.Services.AddControllers();

// Configure Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=consensusdb;Username=consensus_user;Password=consensus_password;Port=5432";

builder.Services.AddDbContext<ConsensusDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Configure SignalR for Blazor Server and custom hubs
builder.Services.AddSignalR(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors = true;
    }
    options.MaximumReceiveMessageSize = 102400; // 100KB
    options.StreamBufferCapacity = 10;
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

// Configure CORS for SignalR and Blazor Server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:5027", 
                "https://localhost:5028", 
                "http://localhost:3000", 
                "https://localhost:3000",
                "http://127.0.0.1:3000",
                "https://127.0.0.1:3000"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ConsensusDbContext>()
.AddDefaultTokenProviders();

// Configure authentication cookies with container-friendly settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow non-HTTPS in container
    options.Cookie.HttpOnly = true;
});

// Flow AuthenticationState as a global cascading value. Required so that
// AuthorizeView / [Authorize] inside @rendermode InteractiveServer pages
// (e.g. Simulations.razor) can see the current user — the wrapper
// <CascadingAuthenticationState> in Routes.razor only covers the SSR
// render tree, not the interactive circuit. Without this the
// "New Simulation" button stays dead with a yellow circuit-error banner.
builder.Services.AddCascadingAuthenticationState();

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    // Define role-based policies
    options.AddPolicy("ViewerOrHigher", policy => policy.RequireRole("Viewer", "Operator", "Admin"));
    options.AddPolicy("OperatorOrHigher", policy => policy.RequireRole("Operator", "Admin"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    
    // Define permission-based policies
    options.AddPolicy("CanViewSimulations", policy => policy.RequireClaim("permission", "view_simulations"));
    options.AddPolicy("CanRunSimulations", policy => policy.RequireClaim("permission", "run_simulations"));
    options.AddPolicy("CanManageUsers", policy => policy.RequireClaim("permission", "manage_users"));
});

// Register core services and interfaces
builder.Services.AddScoped<Consensus.Core.Interfaces.ISimulationService, SimulationService>();
// TODO: Implement additional services in future phases
// builder.Services.AddScoped<IBlockValidator, BlockValidator>();
// builder.Services.AddScoped<IConsensusProtocol, PoetProtocol>();

// Register repositories
builder.Services.AddScoped<Consensus.Core.Repositories.ISimulationRunRepository, Consensus.Data.Repositories.SimulationRunRepository>();
builder.Services.AddScoped<Consensus.Core.Repositories.INodeRepository, Consensus.Data.Repositories.NodeRepository>();
builder.Services.AddScoped<Consensus.Core.Repositories.IBlockRepository, Consensus.Data.Repositories.BlockRepository>();
builder.Services.AddScoped<Consensus.Core.Repositories.IConsensusRoundRepository, Consensus.Data.Repositories.ConsensusRoundRepository>();
builder.Services.AddScoped<Consensus.Core.Repositories.IEventLogRepository, Consensus.Data.Repositories.EventLogRepository>();

// Register analytics service
builder.Services.AddScoped<Consensus.Core.Services.IAnalyticsService, Consensus.Core.Services.AnalyticsService>();

// Register user management service
builder.Services.AddScoped<Consensus.Core.Interfaces.IUserService, Consensus.Data.Services.UserService>();

// Register payload services
builder.Services.AddScoped<Consensus.Core.Services.Payloads.ISupplyChainService, Consensus.Core.Services.Payloads.SupplyChainService>();
builder.Services.AddScoped<Consensus.Core.Services.Payloads.IFederatedLearningService, Consensus.Core.Services.Payloads.FederatedLearningService>();
builder.Services.AddScoped<Consensus.Core.Services.IPayloadService, Consensus.Core.Services.PayloadService>();

// Register Chart.js service for interactive charts
builder.Services.AddScoped<ChartJsService>();

// Register HTTP clients pointing at the Consensus.Api host. In Docker compose
// the URL is `http://api:8080`; for `dotnet run` against a locally-started Api
// it falls back to `http://localhost:5101` (see Api/Properties/launchSettings.json).
var apiBaseUrl = builder.Configuration["ConsensusApi:BaseUrl"] ?? "http://localhost:5101";

builder.Services.AddHttpClient<IBlockExplorerService, BlockExplorerService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl + (apiBaseUrl.EndsWith('/') ? "" : "/"));
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IConsensusApiClient, ConsensusApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl + (apiBaseUrl.EndsWith('/') ? "" : "/"));
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register SimContext as a factory since it's instantiated per simulation
// SimContext requires simulation-specific parameters, so it will be created manually when needed
// builder.Services.AddTransient<SimContext>(); // Commented out - created manually per simulation

// Register database initialization service
builder.Services.AddScoped<DatabaseInitializationService>();

// MudBlazor: component services + per-circuit theme state.
MudBlazor.Services.ServiceCollectionExtensions.AddMudServices(builder.Services);
builder.Services.AddScoped<Consensus.Web.Services.ThemeService>();

// Configure logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    
    if (builder.Environment.IsDevelopment())
    {
        logging.SetMinimumLevel(LogLevel.Debug);
    }
    else
    {
        logging.SetMinimumLevel(LogLevel.Information);
    }
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Consensus Mechanism Simulator API",
        Version = "v1",
        Description = "API for managing blockchain consensus simulations",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Consensus Simulator Team",
            Email = "consensus@simulator.dev"
        }
    });

    // Configure API Key authentication
    options.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "API Key needed to access the endpoints. X-API-Key: your-api-key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-API-Key",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            new string[] {}
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    
    // Use antiforgery only in production
    app.UseAntiforgery();
}
else
{
    app.UseDeveloperExceptionPage();
    
    // Skip antiforgery validation in development/container environment
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/Account"))
        {
            context.Items["__IgnoreAntiforgeryToken"] = true;
        }
        await next();
    });
}

// Configure consensus simulator middleware pipeline
app.UseConsensusSimulatorMiddleware(app.Environment.IsDevelopment());

app.UseHttpsRedirection();
app.UseStaticFiles();

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Consensus Simulator API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Consensus Simulator API Documentation";
        options.DisplayRequestDuration();
    });
}

// Enable CORS
app.UseCors();

// Authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Configure antiforgery middleware (required even if bypassed)
app.UseAntiforgery();

// Map SignalR hub
app.MapHub<SimulationHub>("/simulationHub");

// Map API controllers
app.MapControllers();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Apply EF migrations (controlled by ConsensusSimulator:AutoMigrateDatabase) and seed admin user.
// Order matters: migrate before seeding so Identity tables exist.
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbInit = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
        await dbInit.InitializeAsync();

        var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<IdentitySeeder>>();
        var seeder = new IdentitySeeder(scope.ServiceProvider, logger);
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database");
        throw;
    }
}

app.Run();

// Make Program class accessible for testing
public partial class Program { }
