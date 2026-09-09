using Libreria.Web.Data;
using Libreria.Web.Pages.Categorias.Repositories;
using Libreria.Web.Pages.Categorias.Services;
using Libreria.Web.Pages.Marcas.Repositories;
using Libreria.Web.Pages.Marcas.Services;
using Libreria.Web.Pages.Historico.Repositories;
using Libreria.Web.Pages.Productos.Services;
using Libreria.Web.Pages.Productos.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorPages()
    .AddMvcOptions(options =>
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<CategoriaValidator>();
builder.Services.AddScoped<IMarcaRepository, MarcaRepository>();
builder.Services.AddScoped<MarcaValidator>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<ICatalogoProductoRepository, CatalogoProductoRepository>();
builder.Services.AddScoped<CostoProductoService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<ProductoValidator>();
builder.Services.AddScoped<IHistoricoCostoRepository, HistoricoCostoRepository>();

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
