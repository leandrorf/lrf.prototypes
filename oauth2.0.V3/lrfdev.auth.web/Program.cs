var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddHttpClient("auth-api", (sp, client) =>
{
    var baseUrl = builder.Configuration["AuthApi:BaseUrl"] ?? "https://localhost:7286";
    client.BaseAddress = new Uri(baseUrl);
}).ConfigurePrimaryHttpMessageHandler(sp =>
{
    var handler = new HttpClientHandler();
    if (sp.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }

    return handler;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Erros visíveis no navegador (útil ao testar no celular na rede local).
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Em Development não forçar HTTPS: no celular o certificado de dev não é confiável e o bind HTTPS
// costuma ser só em localhost — o redirect quebrava http://IP_DA_REDE:5067 (tela em branco).
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseStaticFiles();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
