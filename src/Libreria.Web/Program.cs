using Libreria.Web.Data;
using Libreria.Web.Data.Factories;
using Libreria.Web.Domain.Entities;
using Libreria.Web.Business.Validators;
using Libreria.Web.Pages.Historico.Repositories;
using Libreria.Web.Pages.Productos.Services;
using Libreria.Web.Pages.Productos.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorPages()
    .AddMvcOptions(options =>
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

// Fábricas base del Factory Method
builder.Services.AddScoped<CrudRepositoryFactory<Categoria>, CategoriaRepositoryFactory>();
builder.Services.AddScoped<CrudRepositoryFactory<Marca>, MarcaRepositoryFactory>();
builder.Services.AddScoped<CrudRepositoryFactory<Producto>, ProductoRepositoryFactory>();

// Validadores
builder.Services.AddScoped<CategoriaValidator>();
builder.Services.AddScoped<MarcaValidator>();
builder.Services.AddScoped<ProductoValidator>();

// Servicios y Repositorios adicionales (Catálogo, Histórico)
builder.Services.AddScoped<ICatalogoProductoRepository, CatalogoProductoRepository>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IHistoricoCostoRepository, HistoricoCostoRepository>();

var connectionString = builder.Configuration.GetConnectionString("LibreriaDb")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión 'LibreriaDb' en appsettings.json");
var connectionStringSingleton = ConnectionStringSingleton.Instancia;
connectionStringSingleton.Configurar(connectionString);

builder.Services.AddSingleton(connectionStringSingleton);
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();

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
