using System.Data.Common;
using Libreria.Servicio.Proveedores.Application.Models;
using Libreria.Servicio.Proveedores.Application.Ports.Secondary;
using Libreria.Servicio.Proveedores.Domain;
using Microsoft.Data.SqlClient;
using Libreria.Servicio.Proveedores.Infrastructure.Connection;

namespace Libreria.Servicio.Proveedores.Infrastructure.Repositories;

/// <summary>Adaptador SQL Server. El proveedor y su contacto se escriben en una sola transacción local.</summary>
public sealed class ProveedorRepository : IProveedorRepository
{
    private const string Columnas = @"p.ProveedorId, p.PublicId, p.Nit, p.RazonSocial, p.Telefono, p.Correo,
        p.Estado, p.FechaCreacion, p.UsuarioCreacionId, p.FechaModificacion, p.UsuarioModificacionId,
        c.ContactoProveedorId, c.Nombre AS ContactoNombre, c.Cargo AS ContactoCargo,
        c.Telefono AS ContactoTelefono, c.Correo AS ContactoCorreo";
    private readonly IDbConnectionFactory _conexiones;
    private readonly IUsuarioActual _usuario;

    public ProveedorRepository(IDbConnectionFactory conexiones, IUsuarioActual usuario)
    {
        _conexiones = conexiones;
        _usuario = usuario;
    }

    public async Task<IReadOnlyList<Proveedor>> ListarAsync(FiltroProveedores filtro)
    {
        await using var cn = _conexiones.CreateConnection();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = $@"SELECT {Columnas}
            FROM dbo.Proveedor p
            LEFT JOIN dbo.ContactoProveedor c ON c.ProveedorId=p.ProveedorId AND c.EsPrincipal=1
            WHERE (@Estado IS NULL OR p.Estado=@Estado)
              AND (@Busqueda IS NULL OR p.Nit LIKE '%' + @Busqueda + '%' OR p.RazonSocial LIKE '%' + @Busqueda + '%'
                   OR p.Correo LIKE '%' + @Busqueda + '%' OR c.Nombre LIKE '%' + @Busqueda + '%')
            ORDER BY {ColumnaOrden(filtro.Orden)} {(filtro.Descendente ? "DESC" : "ASC")}, p.ProveedorId";
        Parametro(cmd, "@Estado", filtro.Estado.HasValue ? filtro.Estado.Value : DBNull.Value);
        Parametro(cmd, "@Busqueda", string.IsNullOrWhiteSpace(filtro.Busqueda) ? DBNull.Value : filtro.Busqueda.Trim());
        return await LeerAsync(cmd);
    }

    public async Task<Proveedor?> ObtenerPorPublicIdAsync(Guid publicId)
    {
        await using var cn = _conexiones.CreateConnection();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = $@"SELECT {Columnas}
            FROM dbo.Proveedor p
            LEFT JOIN dbo.ContactoProveedor c ON c.ProveedorId=p.ProveedorId AND c.EsPrincipal=1
            WHERE p.PublicId=@PublicId";
        Parametro(cmd, "@PublicId", publicId);
        return (await LeerAsync(cmd)).SingleOrDefault();
    }

    public async Task CrearAsync(Proveedor proveedor)
    {
        await using var cn = _conexiones.CreateConnection();
        await using var tx = await cn.BeginTransactionAsync();
        try
        {
            await using var cmd = cn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT dbo.Proveedor
                (PublicId,Nit,RazonSocial,Telefono,Correo,Estado,FechaCreacion,UsuarioCreacionId)
                OUTPUT INSERTED.ProveedorId
                VALUES(@PublicId,@Nit,@RazonSocial,@Telefono,@Correo,1,SYSUTCDATETIME(),@Usuario);
                ";
            DatosProveedor(cmd, proveedor);
            Parametro(cmd, "@PublicId", proveedor.PublicId);
            Parametro(cmd, "@Usuario", UsuarioId());
            proveedor.ProveedorId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            await GuardarContactoAsync(cn, tx, proveedor);
            await tx.CommitAsync();
        }
        catch (SqlException ex) when (EsNitDuplicado(ex))
        {
            await tx.RollbackAsync();
            throw new ValorDuplicadoException(IProveedorRepository.ColumnaNit, ex);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> ActualizarAsync(Proveedor proveedor)
    {
        await using var cn = _conexiones.CreateConnection();
        await using var tx = await cn.BeginTransactionAsync();
        try
        {
            await using var cmd = cn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"UPDATE dbo.Proveedor SET Nit=@Nit,RazonSocial=@RazonSocial,Telefono=@Telefono,
                Correo=@Correo,FechaModificacion=SYSUTCDATETIME(),UsuarioModificacionId=@Usuario
                WHERE PublicId=@PublicId AND Estado=1";
            DatosProveedor(cmd, proveedor);
            Parametro(cmd, "@PublicId", proveedor.PublicId);
            Parametro(cmd, "@Usuario", UsuarioId());
            if (await cmd.ExecuteNonQueryAsync() == 0)
            {
                await tx.RollbackAsync();
                return false;
            }
            await GuardarContactoAsync(cn, tx, proveedor);
            await tx.CommitAsync();
            return true;
        }
        catch (SqlException ex) when (EsNitDuplicado(ex))
        {
            await tx.RollbackAsync();
            throw new ValorDuplicadoException(IProveedorRepository.ColumnaNit, ex);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> CambiarEstadoAsync(Guid publicId, bool nuevoEstado)
    {
        await using var cn = _conexiones.CreateConnection();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = @"UPDATE dbo.Proveedor SET Estado=@Estado,FechaModificacion=SYSUTCDATETIME(),
            UsuarioModificacionId=@Usuario WHERE PublicId=@PublicId";
        Parametro(cmd, "@Estado", nuevoEstado);
        Parametro(cmd, "@Usuario", UsuarioId());
        Parametro(cmd, "@PublicId", publicId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> ExisteValorAsync(string columna, string valor, Guid? excluirPublicId = null)
    {
        if (columna != IProveedorRepository.ColumnaNit) throw new ArgumentException("Columna no válida.", nameof(columna));
        await using var cn = _conexiones.CreateConnection();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = @"SELECT COUNT(1) FROM dbo.Proveedor
            WHERE Nit=@Valor AND (@Excluir IS NULL OR PublicId<>@Excluir)";
        Parametro(cmd, "@Valor", valor.Trim());
        Parametro(cmd, "@Excluir", excluirPublicId.HasValue ? excluirPublicId.Value : DBNull.Value);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
    }

    private static async Task GuardarContactoAsync(DbConnection cn, DbTransaction tx, Proveedor proveedor)
    {
        var contacto = proveedor.ContactoPrincipal ?? throw new InvalidOperationException("Falta el contacto principal.");
        await using var cmd = cn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"IF EXISTS(SELECT 1 FROM dbo.ContactoProveedor WHERE ProveedorId=@ProveedorId AND EsPrincipal=1)
            UPDATE dbo.ContactoProveedor SET Nombre=@Nombre,Cargo=@Cargo,Telefono=@Telefono,Correo=@Correo
            WHERE ProveedorId=@ProveedorId AND EsPrincipal=1;
            ELSE INSERT dbo.ContactoProveedor(ProveedorId,Nombre,Cargo,Telefono,Correo,EsPrincipal)
            VALUES(@ProveedorId,@Nombre,@Cargo,@Telefono,@Correo,1);";
        Parametro(cmd, "@ProveedorId", proveedor.ProveedorId);
        Parametro(cmd, "@Nombre", contacto.Nombre);
        Parametro(cmd, "@Cargo", contacto.Cargo ?? (object)DBNull.Value);
        Parametro(cmd, "@Telefono", contacto.Telefono ?? (object)DBNull.Value);
        Parametro(cmd, "@Correo", contacto.Correo ?? (object)DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<IReadOnlyList<Proveedor>> LeerAsync(DbCommand cmd)
    {
        var lista = new List<Proveedor>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var proveedor = new Proveedor
            {
                ProveedorId = reader.GetInt32(reader.GetOrdinal("ProveedorId")),
                PublicId = reader.GetGuid(reader.GetOrdinal("PublicId")),
                Nit = reader.GetString(reader.GetOrdinal("Nit")),
                RazonSocial = reader.GetString(reader.GetOrdinal("RazonSocial")),
                Telefono = Texto(reader, "Telefono"),
                Correo = Texto(reader, "Correo"),
                Estado = reader.GetBoolean(reader.GetOrdinal("Estado")),
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                UsuarioCreacionId = GuidNulo(reader, "UsuarioCreacionId"),
                FechaModificacion = FechaNula(reader, "FechaModificacion"),
                UsuarioModificacionId = GuidNulo(reader, "UsuarioModificacionId")
            };
            if (!reader.IsDBNull(reader.GetOrdinal("ContactoProveedorId")))
            {
                proveedor.ContactoPrincipal = new ContactoProveedor
                {
                    ContactoProveedorId = reader.GetInt32(reader.GetOrdinal("ContactoProveedorId")),
                    ProveedorId = proveedor.ProveedorId,
                    Nombre = reader.GetString(reader.GetOrdinal("ContactoNombre")),
                    Cargo = Texto(reader, "ContactoCargo"),
                    Telefono = Texto(reader, "ContactoTelefono"),
                    Correo = Texto(reader, "ContactoCorreo"),
                    EsPrincipal = true
                };
            }
            lista.Add(proveedor);
        }
        return lista;
    }

    private static void DatosProveedor(DbCommand cmd, Proveedor p)
    {
        Parametro(cmd, "@Nit", p.Nit); Parametro(cmd, "@RazonSocial", p.RazonSocial);
        Parametro(cmd, "@Telefono", p.Telefono ?? (object)DBNull.Value); Parametro(cmd, "@Correo", p.Correo ?? (object)DBNull.Value);
    }
    private static string ColumnaOrden(OrdenProveedores o) => o switch
    { OrdenProveedores.Nit => "p.Nit", OrdenProveedores.Correo => "p.Correo", OrdenProveedores.FechaCreacion => "p.FechaCreacion", _ => "p.RazonSocial" };
    private Guid UsuarioId() => _usuario.UsuarioId ?? throw new InvalidOperationException("No existe usuario actual para auditoría.");
    private static bool EsNitDuplicado(SqlException ex) => ex.Number is 2601 or 2627 && ex.Message.Contains("UQ_Proveedor_Nit", StringComparison.OrdinalIgnoreCase);
    private static string? Texto(DbDataReader r, string c) { var i = r.GetOrdinal(c); return r.IsDBNull(i) ? null : r.GetString(i); }
    private static Guid? GuidNulo(DbDataReader r, string c) { var i = r.GetOrdinal(c); return r.IsDBNull(i) ? null : r.GetGuid(i); }
    private static DateTime? FechaNula(DbDataReader r, string c) { var i = r.GetOrdinal(c); return r.IsDBNull(i) ? null : r.GetDateTime(i); }
    private static void Parametro(DbCommand cmd, string nombre, object valor) { var p = cmd.CreateParameter(); p.ParameterName = nombre; p.Value = valor; cmd.Parameters.Add(p); }
}
