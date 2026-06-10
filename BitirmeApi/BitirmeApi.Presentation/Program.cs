using BitirmeApi.Business.ServiceRegistration;
using BitirmeApi.DataAccess.Concrete.EntityFramework.Context;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

// DbContext
builder.Services.AddDbContext<ProjectDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Memory Cache (KTUN servis tokenı için)
builder.Services.AddMemoryCache();

// HttpContextAccessor (UniversityApiService'in JWT claim'lerini okuyabilmesi için)
builder.Services.AddHttpContextAccessor();

// JWT Authentication
// MÜDEK JWT tokenları (öğrenci/öğretim elemanı) kendi secret key'imizle imzalanmış ve doğrulanır.
// Admin tokenları üniversite API'sinden gelir; imza doğrulaması yapılmaz (fallback).
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? string.Empty;
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MudekApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MudekClient";
var mudekSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.UseSecurityTokenValidators = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = !isDevelopment,
        ValidateIssuerSigningKey = false,
        RequireSignedTokens = false,
        RequireExpirationTime = false,
        // MÜDEK tokenları imzalı: önce kendi key'imizle doğrulamayı dene.
        // Admin/üniversite tokenları imzasız geçebilir (fallback).
        SignatureValidator = (token, _) =>
        {
            var handler = new JwtSecurityTokenHandler();
            if (!string.IsNullOrEmpty(jwtSecretKey))
            {
                try
                {
                    handler.ValidateToken(token, new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = mudekSigningKey,
                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,
                        ValidateAudience = true,
                        ValidAudience = jwtAudience,
                        ValidateLifetime = false,
                        RequireExpirationTime = false
                    }, out var validatedToken);
                    return validatedToken;
                }
                catch
                {
                    // MÜDEK doğrulama başarısız — admin/üniversite token'ı olabilir, fallback ile devam
                }
            }
            return handler.ReadJwtToken(token);
        },
        LifetimeValidator = (notBefore, expires, securityToken, _) =>
        {
            var now = DateTime.UtcNow;
            var validTo = expires
                ?? (securityToken as JwtSecurityToken)?.ValidTo
                ?? DateTime.MinValue;
            if (validTo == DateTime.MinValue) return true;
            return validTo.Add(TimeSpan.FromMinutes(5)) > now;
        },
        ClockSkew = TimeSpan.FromMinutes(isDevelopment ? 30 : 5)
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = ctx =>
        {
            var logger = ctx.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();
            var http = ctx.HttpContext;
            var hasAuth = http.Request.Headers.Authorization.Count > 0;
            logger.LogWarning(
                "JWT doğrulama başarısız: {Path} AuthorizationVar={HasAuth} Hata={Error}",
                http.Request.Path.Value,
                hasAuth,
                ctx.Exception.Message);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger with JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MÜDEK API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Login endpoint'inden alınan token'ı buraya yapıştır (Bearer prefix gerekmez).",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Business layer
builder.Services.BusinessRegister(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Geliştirmede HTTP (5010) → HTTPS yönlendirmesi, tarayıcının Authorization başlığını düşürebilir; Vite proxy http kullanır.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Migration (geliştirmede otomatik)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    if (app.Environment.IsDevelopment())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
            await db.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Migration uygulanamadı");
            throw;
        }
    }
}

app.Run();
