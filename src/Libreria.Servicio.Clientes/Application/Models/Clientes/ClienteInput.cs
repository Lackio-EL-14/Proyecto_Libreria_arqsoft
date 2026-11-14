namespace Libreria.Servicio.Clientes.Application.Models;

/// <summary>Datos que el usuario escribe para dar de alta o editar un cliente.</summary>
public class ClienteInput
{
    public string CiNit { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? Correo { get; set; }
}
