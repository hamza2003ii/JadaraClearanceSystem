using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
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
// 2. Routing Configuration (Lowercase URLs)
// -----------------------------------------------------------------------------
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// -----------------------------------------------------------------------------
// 3. JWT Bearer Authentication & Authorization Setup
// -----------------------------------------------------------------------------
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
    ?? jwtSettings["Key"]
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
// 4. Dependency Injection (Helpers, Repositories, Services, CORS)
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

// Rate Limiting Configuration
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Policy for Auth endpoints (login / register) - 15 requests per minute per IP
    options.AddPolicy("AuthRateLimit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_client",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 15,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // General API rate limit - 60 requests per minute
    options.AddPolicy("GeneralApiRateLimit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_client",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }));
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
// 5. Swagger Setup with JWT Support & XML Documentation
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
// 6. HTTP Request Pipeline & Middleware Setup
// -----------------------------------------------------------------------------

// Security Headers Middleware (OWASP compliance)
app.UseMiddleware<SecurityHeadersMiddleware>();

// Global Exception Handler Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Enable CORS
app.UseCors("AllowAll");

// Enable Rate Limiting
app.UseRateLimiter();

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

// Map API Controllers & Health Check Endpoint
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "Healthy", 
    system = "Jadara Clearance & Service Management Platform",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow 
}));

// Initialize Database and Seed Default Roles/Departments
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<JadaraClearanceDbContext>();
        context.Database.EnsureCreated();

        if (!context.Roles.Any())
        {
            context.Roles.AddRange(
                new Role { RoleName = "Student" },
                new Role { RoleName = "DepartmentOfficer" },
                new Role { RoleName = "Admin" }
            );
        }

        if (!context.Departments.Any())
        {
            context.Departments.AddRange(
                new Department { DepartmentName = "Library", RequiresPayment = false },
                new Department { DepartmentName = "Finance", RequiresPayment = true },
                new Department { DepartmentName = "Registration", RequiresPayment = false },
                new Department { DepartmentName = "Student Affairs", RequiresPayment = false }
            );
        }

        context.SaveChanges();

        // Seed Default Stakeholder Accounts (Admin, Student, Department Officers)
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var studentRole = context.Roles.First(r => r.RoleName == "Student");
        var officerRole = context.Roles.First(r => r.RoleName == "DepartmentOfficer");
        var adminRole = context.Roles.First(r => r.RoleName == "Admin");

        var libraryDept = context.Departments.First(d => d.DepartmentName == "Library");
        var financeDept = context.Departments.First(d => d.DepartmentName == "Finance");
        var regDept = context.Departments.First(d => d.DepartmentName == "Registration");
        var affairsDept = context.Departments.First(d => d.DepartmentName == "Student Affairs");

        var existingStudent = context.Users.FirstOrDefault(u => u.Email == "student@jadara.edu");
        if (existingStudent != null)
        {
            existingStudent.FullName = "Hamza Mohammad Sadeq";
        }

        if (!context.Users.Any())
        {
            context.Users.AddRange(
                // Administrator
                new User
                {
                    FullName = "System Administrator",
                    Email = "admin@jadara.edu",
                    PasswordHash = passwordHasher.HashPassword("Admin123!"),
                    RoleId = adminRole.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                // Student
                new User
                {
                    FullName = "Hamza Mohammad Sadeq",
                    Email = "student@jadara.edu",
                    PasswordHash = passwordHasher.HashPassword("Student123!"),
                    RoleId = studentRole.Id,
                    UniversityId = "20241001",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                // Officers
                new User
                {
                    FullName = "Library Officer",
                    Email = "library@jadara.edu",
                    PasswordHash = passwordHasher.HashPassword("Officer123!"),
                    RoleId = officerRole.Id,
                    DepartmentId = libraryDept.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    FullName = "Finance Officer",
                    Email = "finance@jadara.edu",
                    PasswordHash = passwordHasher.HashPassword("Officer123!"),
                    RoleId = officerRole.Id,
                    DepartmentId = financeDept.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    FullName = "Registration Officer",
                    Email = "registration@jadara.edu",
                    PasswordHash = passwordHasher.HashPassword("Officer123!"),
                    RoleId = officerRole.Id,
                    DepartmentId = regDept.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    FullName = "Student Affairs Officer",
                    Email = "affairs@jadara.edu",
                    PasswordHash = passwordHasher.HashPassword("Officer123!"),
                    RoleId = officerRole.Id,
                    DepartmentId = affairsDept.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );

            context.SaveChanges();
        }

        logger.LogInformation("Database 'JadaraClearanceDB' ensured and seeded successfully with default stakeholders.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while ensuring or seeding the database.");
    }
}

app.Run();