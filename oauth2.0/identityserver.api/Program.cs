using System.Net;
using System.Text;
using identityserver.api.Configuration;
using identityserver.api.Data;
using identityserver.api.Models;
using identityserver.api.Services;
using identityserver.api.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<LoginUiOptions>(builder.Configuration.GetSection(LoginUiOptions.SectionName));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<TokenEndpointOperationFilter>();
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Token JWT obtido via OAuth (ex.: /connect/authorize + /connect/token). Ex: Bearer {seu_token}"
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
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IOAuthService, OAuthService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                  ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("perm:app.demo.read", p => p.RequireClaim("permission", "app.demo.read"));
    options.AddPolicy("perm:app.demo.write", p => p.RequireClaim("permission", "app.demo.write"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Seed de cliente padrão para desenvolvimento (meu-app / secret)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var (hash, salt) = hasher.HashPassword("secret");

    var client = await db.Clients.FirstOrDefaultAsync(c => c.ClientId == "meu-app");
    if (client is null)
    {
        db.Clients.Add(new Client
        {
            ClientId = "meu-app",
            ClientSecretHash = hash,
            ClientSecretSalt = salt,
            Name = "App desenvolvimento",
            RedirectUris = "http://localhost:5235/connect/callback-demo,https://app.example.com/callback",
            AllowedGrantTypes = "authorization_code,refresh_token"
        });
    }
    else
    {
        client.ClientSecretHash = hash;
        client.ClientSecretSalt = salt;
    }
    await db.SaveChangesAsync();

    await AuthDbDevelopmentSeed.SeedRbacAndDevicesAsync(db);
}

// Página de callback apenas em desenvolvimento (exibe code/state para testar o fluxo)
if (app.Environment.IsDevelopment())
{
    app.MapGet("/connect/callback-demo", (HttpContext ctx) =>
    {
        var code = ctx.Request.Query["code"].FirstOrDefault() ?? "";
        var state = ctx.Request.Query["state"].FirstOrDefault() ?? "";
        var html = $@"<!DOCTYPE html><html><head><meta charset=""utf-8""/><title>Callback (dev)</title></head><body>
<h2>Callback (apenas desenvolvimento)</h2>
<p><strong>code:</strong> <code>{WebUtility.HtmlEncode(code)}</code></p>
<p><strong>state:</strong> <code>{WebUtility.HtmlEncode(state)}</code></p>
<p>Use o <code>code</code> em POST /connect/token (grant_type=authorization_code).</p>
</body></html>";
        return Results.Content(html, "text/html; charset=utf-8");
    });
}

app.Run();
