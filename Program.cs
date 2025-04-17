using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MiddlewareDemo.Data;
using MiddlewareDemo.Services;
using middleware_demo.Middlewares; 
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ✅ Ensure Logs Directory is Created
var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
if (!Directory.Exists(logDirectory))
{
    Directory.CreateDirectory(logDirectory);
}
var logPath = Path.Combine(logDirectory, "log.txt");

// ✅ Configure Serilog for Logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

try
{
    Log.Information("🚀 Application is starting...");

    // ✅ Load Configuration
    var configuration = builder.Configuration ?? throw new InvalidOperationException("Configuration is missing.");

    // ✅ Configure Database Context
    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Database connection string is missing.");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 41))));

    // ✅ Register Services
    builder.Services.AddScoped<AuthService>();

    // ✅ Configure JWT Authentication
    var secretKey = configuration["JwtSettings:Secret"]
        ?? throw new InvalidOperationException("JWT Secret is missing.");
    var key = Encoding.UTF8.GetBytes(secretKey);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = configuration["JwtSettings:Issuer"] ?? "default-issuer",
                ValidAudience = configuration["JwtSettings:Audience"] ?? "default-audience",
                ClockSkew = TimeSpan.Zero //  Ensure token expires exactly when it should
            };
        });

    // ✅ Enable CORS for Frontend Access
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll",
            policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    });

    // ✅ Configure Swagger with JWT Authentication
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Middleware API", Version = "v1" });

        // ✅ Add JWT Bearer Authentication in Swagger UI
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter 'Bearer <your-token>' (without quotes) to authorize."
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // ✅ Ensure API Uses Newtonsoft.Json for JSON Patch Support
    builder.Services.AddControllers()
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
        });

    // ✅ Suppress Default Model State Errors
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });

    // ✅ Register Middleware for HTTP Context
    builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

    var app = builder.Build();

    // ✅ Run Automatic Database Migrations
    EnsureDatabaseMigrated(app);

    // ✅ Global Error Handling Middleware
    app.UseExceptionHandler("/error");

    // ✅ Enable CORS
    app.UseCors("AllowAll");

    // ✅ Use Custom Middleware for Logging
    app.UseMiddleware<RequestLoggingMiddleware>();

    // ✅ Enable Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // ✅ Map API Controllers
    app.MapControllers();

    // ✅ Enable Swagger UI in Development Mode
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // ✅ Start Application
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Application startup failed.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// ✅ **Ensures the Database is Migrated Automatically**
static void EnsureDatabaseMigrated(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); 
    dbContext.Database.Migrate();
    Log.Information("✅ Database migrated successfully.");
}



