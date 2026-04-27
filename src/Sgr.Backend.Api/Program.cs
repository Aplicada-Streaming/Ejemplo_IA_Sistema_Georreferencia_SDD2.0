using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Sgr.Backend.Api.Middleware;
using Sgr.Backend.Api.Startup;
using Sgr.Modules.Identity;
using Sgr.Modules.Identity.Authentication;
using Sgr.Modules.Surveys;
using Sgr.Modules.Sync;
using Sgr.Modules.Templates;
using Sgr.Persistence;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}"));

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");

    builder.Services
        .AddSgrPersistence(connectionString)
        .AddSgrIdentity(builder.Configuration)
        .AddSgrTemplates()
        .AddSgrSurveys()
        .AddSgrSync();

    builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection(SeedOptions.SectionName));

    builder.Services.AddControllers()
        .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer();

    // Configure JwtBearer lazily via IOptions so that test/factory overrides of JwtOptions
    // are honored (WebApplicationFactory injects config after CreateBuilder runs).
    builder.Services
        .AddOptions<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<Microsoft.Extensions.Options.IOptions<JwtOptions>, IWebHostEnvironment>((bearer, jwtOpts, env) =>
        {
            var jwt = jwtOpts.Value;
            bearer.RequireHttpsMetadata = !env.IsDevelopment();
            bearer.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                ClockSkew = TimeSpan.FromSeconds(30),
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(o =>
    {
        o.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "SGR Backend API",
            Version = "v1",
            Description = "Sistema de Gestión de Relevamientos Georreferenciados — Vialidad",
        });
        o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "JWT Bearer token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        });
        o.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        });
    });

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SGR Backend API v1"));
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", at = DateTime.UtcNow }));

    using (var scope = app.Services.CreateScope())
    {
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<SgrDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var clock = sp.GetRequiredService<Sgr.Domain.Common.IDateTimeProvider>();
        var seedOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SeedOptions>>().Value;
        var seedLogger = sp.GetRequiredService<ILogger<Program>>();
        await DatabaseSeeder.SeedAsync(db, hasher, clock, seedOptions, seedLogger);
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application startup failed.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
