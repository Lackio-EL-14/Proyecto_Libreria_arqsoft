namespace Libreria.Servicio.Clientes.Application.Models;

/// <summary>Columnas por las que se puede ordenar el listado (lista cerrada: el SQL nunca recibe texto libre).</summary>
public enum OrdenClientes
{
    RazonSocial,
    CiNit,
    Correo,
    FechaCreacion
}

/// <summary>Filtros del listado de clientes.</summary>
public class FiltroClientes
{
    /// <summary>Texto que se busca en CI/NIT, razón social o correo.</summary>
    public string? Busqueda { get; set; }

    /// <summary><c>true</c> activos, <c>false</c> inactivos, <c>null</c> todos.</summary>
    public bool? Estado { get; set; } = true;

    public OrdenClientes Orden { get; set; } = OrdenClientes.RazonSocial;

    public bool Descendente { get; set; }
}
