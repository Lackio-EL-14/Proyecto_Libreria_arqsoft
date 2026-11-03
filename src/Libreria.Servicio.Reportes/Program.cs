using System.Text.Json.Serialization;
using Libreria.Servicio.Reportes.DependencyInjection;
using Libreria.Servicio.Reportes.Presentation.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ReportExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddServicioReportes(builder.Configuration);

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// El contrato de laboratorio utiliza HTTP en el puerto 5103 y encabezados del Front.
app.UseMiddleware<CurrentUserMiddleware>();
app.MapControllers();
app.Run();

public partial class Program;
