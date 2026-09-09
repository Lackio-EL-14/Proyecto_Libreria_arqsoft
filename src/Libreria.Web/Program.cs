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
builder.Services.AddScoped<MarcaRepository>();
builder.Services.AddScoped<IListadoMarcasRepository>(
    provider => provider.GetRequiredService<MarcaRepository>());
builder.Services.AddScoped<IRegistroMarcaRepository>(
    provider => provider.GetRequiredService<MarcaRepository>());
builder.Services.AddScoped<IEdicionMarcaRepository>(
    provider => provider.GetRequiredService<MarcaRepository>());
builder.Services.AddScoped<IBajaMarcaRepository>(
    provider => provider.GetRequiredService<MarcaRepository>());
builder.Services.AddScoped<IValidadorMarcaRepository>(
    provider => provider.GetRequiredService<MarcaRepository>());
builder.Services.AddScoped<MarcaValidator>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<CatalogoProductoRepository>();
builder.Services.AddScoped<IConsultaCatalogoProductoRepository>(
    provider => provider.GetRequiredService<CatalogoProductoRepository>());
builder.Services.AddScoped<IValidacionCatalogoProductoRepository>(
    provider => provider.GetRequiredService<CatalogoProductoRepository>());
builder.Services.AddScoped<IComparadorCostoProducto, CostoProductoService>();
builder.Services.AddScoped<ProductoService>();
builder.Services.AddScoped<IConsultaProductosService>(
    provider => provider.GetRequiredService<ProductoService>());
builder.Services.AddScoped<IRegistroProductoService>(
    provider => provider.GetRequiredService<ProductoService>());
builder.Services.AddScoped<IEdicionProductoService>(
    provider => provider.GetRequiredService<ProductoService>());
builder.Services.AddScoped<IConsultaProductoDetalleService>(
    provider => provider.GetRequiredService<ProductoService>());
builder.Services.AddScoped<IBajaProductoService>(
    provider => provider.GetRequiredService<ProductoService>());
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
