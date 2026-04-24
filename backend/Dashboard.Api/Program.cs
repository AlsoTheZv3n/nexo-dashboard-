using Dashboard.Api.Auth;
using Dashboard.Api.Middleware;
using Dashboard.Core.Abstractions;
using Dashboard.Infrastructure;
using Dashboard.Infrastructure.Persistence;
using Dashboard.PowerShell;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---- Logging (Serilog) ----
builder.Host.UseSerilog((context, cfg) => cfg
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// ---- Core wiring ----
builder.Services.AddDashboardInfrastructure(builder.Configuration);
builder.Services.AddDashboardPowerShell(builder.Configuration);

// ---- FluentValidation ----
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ---- JWT Auth ----
// TokenValidationParameters are wired late via IPostConfigureOptions so that
// test hosts can still override the Jwt:* configuration in ConfigureWebHost.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();
builder.Services.AddAuthorization();

// ---- Exception handler ----
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ---- Controllers + validation filter ----
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});
builder.Services.AddScoped<ValidationFilter>();

// ---- Swagger / OpenAPI ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "Dashboard API", Version = "v1" });
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT access token."
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });
});

// ---- CORS (Dev only — in Prod gleiche Origin via Nginx) ----
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("dev", p => p
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

// ---- Pipeline ----
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("dev");
}

app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ---- Migrate + seed on startup (Dev/Staging only) ----
if (!app.Environment.IsEnvironment("IntegrationTests"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, hasher);
}

app.Run();

// For WebApplicationFactory<Program> in tests
public partial class Program;
