using Libreria.Web.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// Registramos la fábrica de conexiones ADO.NET como Singleton.
// Cualquier PageModel puede pedirla por inyección de dependencias
// en vez de crear un SqlConnection directamente (Inversión de Dependencias - SOLID).
builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("LibreriaDb")
        ?? throw new InvalidOperationException(
            "Falta la cadena de conexión 'LibreriaDb' en appsettings.json");
    return new SqlConnectionFactory(connectionString);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
