using BitirmeApi.Business.ServiceRegistration;
using BitirmeApi.DataAccess.Concrete.EntityFramework.Context;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

// DbContext
builder.Services.AddDbContext<ProjectDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Üniversite API token'ını doğrudan kabul et — imza doğrulaması yapılmaz.
// Signing key üniversitenin elindedir; biz sadece token'ı decode edip claim'leri okuruz.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Varsayılan JsonWebTokenHandler yolu, SignatureValidator'dan JwtSecurityToken dönünce uyumsuz kalıp 401 verebiliyor.
    options.UseSecurityTokenValidators = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        // Üniversite token'ında exp/nbf farklı saat dilimleri veya kısa ömür geliştirmede sık 401 üretir.
        ValidateLifetime = !isDevelopment,
        ValidateIssuerSigningKey = false,
        RequireSignedTokens = false,
        RequireExpirationTime = false,
        // Login tarafında claim'ler JwtSecurityToken ile okunuyor; JsonWebTokenHandler bazı üretici JWT'lerinde 401 üretebiliyor.
        SignatureValidator = (token, _) => new JwtSecurityTokenHandler().ReadJwtToken(token),
        // SignatureValidator JwtSecurityToken döndürünce default LifetimeValidator notBefore/expires parametrelerini
        // null alıyor ve "missing Expiration Time" (IDX10225) hatası üretiyor. Token'daki exp'i doğrudan okuruz.
        LifetimeValidator = (notBefore, expires, securityToken, _) =>
        {
            var now = DateTime.UtcNow;
            var validTo = expires
                ?? (securityToken as JwtSecurityToken)?.ValidTo
                ?? DateTime.MinValue;
            if (validTo == DateTime.MinValue) return true; // exp yoksa geç (üniversite token'ı zaten kısa ömürlü)
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
