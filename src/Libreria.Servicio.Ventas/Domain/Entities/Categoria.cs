using System;

namespace Libreria.Servicio.Ventas.Domain
{
    public class Categoria
    {
        public int CategoriaId { get; set; } 
        public Guid PublicId { get; set; }   
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string Ubicacion { get; set; } = string.Empty;
        public bool Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? UsuarioCreacionId { get; set; }
        public int? UsuarioModificacionId { get; set; }
    }
}
