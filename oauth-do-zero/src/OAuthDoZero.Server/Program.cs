using Microsoft.EntityFrameworkCore;
using OAuthDoZero.Server.Data;
using OAuthDoZero.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuração do Entity Framework Core + MySQL
builder.Services.AddDbContext<OAuthDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

// Registrar serviços OAuth
builder.Services.AddScoped<ICryptographyService, CryptographyService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IOAuthService, OAuthService>();

// Configuração JWT
var jwtSecret = builder.Configuration["JWT:Secret"] ?? "your-super-secret-key-here-change-in-production-at-least-32-chars-long";
var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? "https://localhost:5000";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = false, // Validamos pelo client_id
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "OAuth do Zero",
        Version = "v1",
        Description = "Servidor de identidade OAuth 2.0 e OpenID Connect"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "OAuth do Zero v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication(); // Adicionar antes de UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();
