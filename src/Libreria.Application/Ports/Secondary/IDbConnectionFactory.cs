using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
using System.Data.Common;

namespace Libreria.Application.Ports.Secondary;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
