using Libreria.Web.Data;
using Libreria.Web.Pages.Categorias.Repositories;
using Libreria.Web.Pages.Productos.Services;
using Libreria.Web.Pages.Productos.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<CostoProductoService>();
builder.Services.AddScoped<ProductoService>();

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
