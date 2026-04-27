using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Logging;
using lrfdev.auth.api.Authorization;
using lrfdev.auth.api.Data;
using lrfdev.auth.api.Security;
using lrfdev.auth.api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Cole **somente o valor do access_token** (sem a palavra `Bearer`). O Swagger envia o header `Authorization: Bearer ...` automaticamente."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not configured.");

builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPermissionResolver, PermissionResolver>();
builder.Services.AddScoped<DbInitializer>();

var jwtKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("JWT signing key is missing.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "lrfdev.auth.api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "lrf.resource.api";

if (builder.Environment.IsDevelopment())
{
    IdentityModelEventSource.ShowPII = true;
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        if (builder.Environment.IsDevelopment())
        {
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtBearer");
                    logger.LogWarning(context.Exception, "Falha ao validar JWT");
                    return Task.CompletedTask;
                }
            };
        }
    });

builder.Services.AddScopeBasedAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Mantém o Bearer após F5 no Swagger (evita 401 por token “sumido”).
        options.EnablePersistAuthorization();
    });
}

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await initializer.InitializeAsync();
}

// Mesmo motivo do projeto web: em Development não redirecionar HTTP→HTTPS ao testar por IP na LAN.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Sem autenticação — use para confirmar que está falando com esta API (se isto falhar, não é JWT).
app.MapGet("/api/ping", () => Results.Ok(new { ok = true, service = "lrfdev.auth.api" }));

app.MapControllers();

app.Run();
