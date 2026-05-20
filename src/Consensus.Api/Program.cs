using Consensus.Core.Interfaces;
using Consensus.Core.Repositories;
using Consensus.Core.Services;
using Consensus.Data;
using Consensus.Data.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── Database ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ConsensusDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Database=consensusdb;Username=consensus_user;Password=consensus_password";
    options.UseNpgsql(connectionString);
});

// ─── Repositories ────────────────────────────────────────────────────────────
builder.Services.AddScoped<ISimulationRunRepository, SimulationRunRepository>();
builder.Services.AddScoped<INodeRepository, NodeRepository>();
builder.Services.AddScoped<IBlockRepository, BlockRepository>();
builder.Services.AddScoped<IConsensusRoundRepository, ConsensusRoundRepository>();
builder.Services.AddScoped<IEventLogRepository, EventLogRepository>();

// ─── Services ────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
// Api host serves READS only; writes throw. The Web host registers the
// runtime-bearing SimulationService — keeping them as separate impls is
// deliberate (see DbBackedSimulationService docstring).
builder.Services.AddScoped<ISimulationService, DbBackedSimulationService>();
// Export service powers SimulationResultsController POST {id}/export. Without
// this registration the controller throws InvalidOperationException at
// activation and the request surfaces as 500 (B-002 from the test report).
builder.Services.AddScoped<ISimulationResultsExportService, SimulationResultsExportService>();
builder.Services.AddScoped<ISimulationMetricsService, SimulationMetricsService>();

// ─── Authentication (permissive in dev so [Authorize] resolves without JWT) ──
// Production should swap this for the same Identity scheme Web uses or a shared
// JWT issuer. For the thesis demo, the Api is internal to the compose network.
builder.Services.AddAuthentication("DevAlwaysAllow")
    .AddScheme<AuthenticationSchemeOptions, DevAlwaysAllowAuthHandler>("DevAlwaysAllow", _ => { });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder("DevAlwaysAllow")
        .RequireAssertion(_ => true)
        .Build();
});

// ─── MVC + OpenAPI ───────────────────────────────────────────────────────────
// IgnoreCycles is required because EF navigation properties form bidirectional
// cycles (SimulationRun.Nodes -> Node.SimulationRun -> SimulationRun.Nodes ...).
// Without this the dashboard's GET /api/v1/Simulations/{id} blew up with
// JsonException mid-stream, the client got malformed JSON, parsing returned
// null, and the page stayed on "Loading Simulation Dashboard..." forever.
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        // SimulationResultsExportService computes throughput as blocks /
        // totalSeconds. Sims that complete in < 1 ms (or short-circuit on
        // failure) make the denominator zero, producing
        // double.PositiveInfinity. The default JSON serializer refuses
        // those values, surfacing as a 500 mid-export. Allow named float
        // literals so the export still serializes (the consumer can
        // sanitise on read).
        o.JsonSerializerOptions.NumberHandling =
            System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddLogging();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()));

var app = builder.Build();

// ─── Auto-migrate so a fresh Postgres comes up healthy ───────────────────────
// Web also runs migrations on its startup; whichever host wins the race applies
// them. Postgres rejects duplicate `__EFMigrationsHistory` rows so the loser is
// a no-op.
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ConsensusDbContext>();
        var autoMigrate = builder.Configuration.GetValue<bool>("ConsensusSimulator:AutoMigrateDatabase", true);
        if (autoMigrate)
        {
            await db.Database.MigrateAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Api startup migration failed; continuing — Web may complete it");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make Program accessible for integration tests.
public partial class Program { }

// Permissive auth handler used in dev to satisfy [Authorize] attributes without
// requiring an Identity round-trip. Stamps every request as an anonymous "Admin"
// so role-gated read endpoints work; replace with JWT for production.
internal sealed class DevAlwaysAllowAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DevAlwaysAllowAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "dev-anonymous"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "Operator"),
            new Claim(ClaimTypes.Role, "Viewer"),
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
