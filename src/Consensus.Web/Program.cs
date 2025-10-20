using Consensus.Web.Components;
using Consensus.Web.Hubs;
using Consensus.Web.Services;
using Consensus.Web.Middleware;
using Consensus.Data;
using Consensus.Core.Interfaces;
using Consensus.Core.Services;
using Microsoft.EntityFrameworkCore;
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
    .AddInteractiveServerComponents();

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

// Configure SignalR
builder.Services.AddSignalR(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors = true;
    }
    options.MaximumReceiveMessageSize = 102400; // 100KB
    options.StreamBufferCapacity = 10;
});

// Configure CORS for SignalR
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5027", "https://localhost:5028")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configure authentication and authorization (placeholder for future implementation)
builder.Services.AddAuthentication()
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
    });

builder.Services.AddAuthorization(options =>
{
    // Define authorization policies
    options.AddPolicy("ViewerOrHigher", policy => policy.RequireRole("Viewer", "Operator", "Admin"));
    options.AddPolicy("OperatorOrHigher", policy => policy.RequireRole("Operator", "Admin"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// Register core services and interfaces
// TODO: Implement actual services in future phases
// builder.Services.AddScoped<ISimulationService, SimulationService>();
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

// Register Chart.js service for interactive charts
builder.Services.AddScoped<ChartJsService>();

// Register HTTP client for Block Explorer API
builder.Services.AddHttpClient<IBlockExplorerService, BlockExplorerService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5101/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register SimContext as a factory since it's instantiated per simulation
// SimContext requires simulation-specific parameters, so it will be created manually when needed
// builder.Services.AddTransient<SimContext>(); // Commented out - created manually per simulation

// Register database initialization service
builder.Services.AddScoped<DatabaseInitializationService>();

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
}
else
{
    app.UseDeveloperExceptionPage();
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

app.UseAntiforgery();

// Map SignalR hub
app.MapHub<SimulationHub>("/simulationHub");

// Map API controllers
app.MapControllers();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
