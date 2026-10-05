namespace Libreria.Application.Domain
{
    public class Cliente
    {
        public int ClienteId { get; set; }
        public string CiNit { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string? Correo { get; set; }
    }
}