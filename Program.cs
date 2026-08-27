using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using JadaraClearance.Helpers;
using JadaraClearance.Middleware;
using JadaraClearance.Models;
using JadaraClearance.Repositories;
using JadaraClearance.Services;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// 1. Database Context Configuration
// -----------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<JadaraClearanceDbContext>(options =>
    options.UseSqlServer(connectionString));

// -----------------------------------------------------------------------------
// 2. JWT Bearer Authentication & Authorization Setup
// -----------------------------------------------------------------------------
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"] 
    ?? throw new InvalidOperationException("JWT Secret Key is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// -----------------------------------------------------------------------------
// 3. Dependency Injection (Helpers, Repositories, Services, CORS)
// -----------------------------------------------------------------------------
builder.Services.AddHttpContextAccessor();

// CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Helpers
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IClearanceRequestRepository, ClearanceRequestRepository>();
builder.Services.AddScoped<IClearanceApprovalRepository, ClearanceApprovalRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClearanceService, ClearanceService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Controllers & JSON Serializer
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// -----------------------------------------------------------------------------
// 4. Swagger Setup with JWT Support & XML Documentation
// -----------------------------------------------------------------------------
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Jadara Clearance System API",
        Version = "v1",
        Description = "ASP.NET Core 8 Web API for managing university student clearance requests, department approvals, and audit logs."
    });

    // XML Documentation Integration
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // JWT Security Definition for Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token in the format: {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

var app = builder.Build();

// -----------------------------------------------------------------------------
// 5. HTTP Request Pipeline & Middleware Setup
// -----------------------------------------------------------------------------

// Global Exception Handler Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Enable CORS
app.UseCors("AllowAll");

// Serve Frontend Static Files
var frontendPath = Path.Combine(builder.Environment.ContentRootPath, "frontend");
if (Directory.Exists(frontendPath))
{
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendPath),
        RequestPath = ""
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendPath),
        RequestPath = ""
    });
}

// Enable Swagger UI at /swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Jadara Clearance System API v1");
    c.RoutePrefix = "swagger"; // Serve Swagger UI at /swagger
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
