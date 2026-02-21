using Microsoft.EntityFrameworkCore;
using OAuthIdentityServer.Configuration;
using OAuthIdentityServer.Data;
using OAuthIdentityServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<OAuthOptions>(builder.Configuration.GetSection(OAuthOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não configurada.");

builder.Services.AddDbContext<IdentityDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions => mySqlOptions.EnableStringComparisonTranslations());
});

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await db.Database.MigrateAsync();
    if (app.Environment.IsDevelopment())
        await SeedData.EnsureSeedAsync(db);
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
