using Consensus.Data;
using Consensus.Data.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Entity Framework and Database
builder.Services.AddDbContext<ConsensusDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Host=localhost;Database=consensus_simulator;Username=postgres;Password=postgres";
    options.UseNpgsql(connectionString);
});

// Add repositories
builder.Services.AddScoped<IEnhancedBlockRepository, BlockRepository>();

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Map controllers
app.MapControllers();

app.Run();

// Make Program class accessible for testing
public partial class Program { }