using Libreria.Web.Data;
using Libreria.Web.Pages.Categorias.Repositories;
using Libreria.Web.Pages.Categorias.Services;
using Libreria.Web.Pages.Marcas.Repositories;
using Libreria.Web.Pages.Marcas.Services;
using Libreria.Web.Pages.Productos.Services;
using Libreria.Web.Pages.Productos.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddScoped<CategoriaRepository>();
builder.Services.AddScoped<IListadoCategoriasRepository>(
    provider => provider.GetRequiredService<CategoriaRepository>());
builder.Services.AddScoped<IRegistroCategoriaRepository>(
    provider => provider.GetRequiredService<CategoriaRepository>());
builder.Services.AddScoped<IEdicionCategoriaRepository>(
    provider => provider.GetRequiredService<CategoriaRepository>());
builder.Services.AddScoped<IBajaCategoriaRepository>(
    provider => provider.GetRequiredService<CategoriaRepository>());
builder.Services.AddScoped<IReactivacionCategoriaRepository>(
    provider => provider.GetRequiredService<CategoriaRepository>());
builder.Services.AddScoped<IValidadorCategoriaRepository>(
    provider => provider.GetRequiredService<CategoriaRepository>());
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
builder.Services.AddScoped<CostoProductoService>();
builder.Services.AddScoped<ProductoService>();
builder.Services.AddScoped<ProductoValidator>();


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
