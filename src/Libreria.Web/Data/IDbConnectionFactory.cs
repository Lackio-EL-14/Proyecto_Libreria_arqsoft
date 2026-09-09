using System.Data.Common;

namespace Libreria.Web.Data;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
