using System.Data.Common;

namespace Libreria.Application.Ports.Secondary;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
