using Libreria.Domain;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Factories;
using System.Data.Common;

namespace Libreria.Servicio.Ventas.Application.Ports.Secondary;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
