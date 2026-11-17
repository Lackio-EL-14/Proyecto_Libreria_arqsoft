using System.Data;
using System.Data.Common;
using Libreria.Servicio.Ventas.Application.Comun;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Primary;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Domain;

namespace Libreria.Servicio.Ventas.Application.Facades;

public sealed class VentaFacade : IVentaFacade
{
    private readonly IVentaRepository _ventas;
    private readonly IProductosPort _productos;
    private readonly IClientesPort _clientes;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IStockFacade _stock;
    private readonly IMovimientoStockRepository _movimientos;

    public VentaFacade(
        IVentaRepository ventas,
        IProductosPort productos,
        IClientesPort clientes,
        IDbConnectionFactory connectionFactory,
        IUsuarioActual usuarioActual,
        IStockFacade stock,
        IMovimientoStockRepository movimientos)
    {
        _ventas = ventas;
        _productos = productos;
        _clientes = clientes;
        _connectionFactory = connectionFactory;
        _usuarioActual = usuarioActual;
        _stock = stock;
        _movimientos = movimientos;
    }

    public async Task<Result<VentaRegistradaResult>> RegistrarVentaAsync(
        RegistrarVentaInput input,
        CancellationToken cancellationToken = default)
    {
        var validacion = ValidarRegistro(input);
        if (!validacion.EsExito)
        {
            return Result<VentaRegistradaResult>.Fallo(validacion);
        }

        var usuario = ValidarUsuarioActual();
        if (!usuario.EsExito)
        {
            return Result<VentaRegistradaResult>.Fallo(usuario);
        }

        var usuarioId = _usuarioActual.UsuarioId!.Value;
        var registrada = await RecuperarVentaAsync(input, usuarioId);
        if (!registrada.EsExito || registrada.Valor is not null)
        {
            return registrada.EsExito
                ? Result<VentaRegistradaResult>.Exito(registrada.Valor!)
                : Result<VentaRegistradaResult>.Fallo(registrada);
        }

        var cliente = await _clientes.ObtenerPorCiNitAsync(
            input.CiNitCliente,
            cancellationToken);
        if (!cliente.EsExito)
        {
            return Result<VentaRegistradaResult>.Fallo(cliente);
        }

        var detalles = await PrepararDetallesAsync(input.Detalles, cancellationToken);
        if (!detalles.EsExito)
        {
            return Result<VentaRegistradaResult>.Fallo(detalles);
        }

        var lineasStock = detalles.Valor
            .Select(detalle => new StockOperacionItem(
                detalle.ProductoPublicId,
                detalle.Cantidad))
            .ToList();
        var descuento = await _stock.DescontarAsync(
            lineasStock,
            input.SolicitudId,
            cancellationToken);
        if (!descuento.EsExito)
        {
            return Result<VentaRegistradaResult>.Fallo(descuento);
        }

        try
        {
            var resultado = await GuardarVentaLocalAsync(
                input,
                cliente.Valor,
                detalles.Valor,
                usuarioId);
            return Result<VentaRegistradaResult>.Exito(resultado);
        }
        catch (Exception error)
        {
            await CompensarOPropagarAsync(
                () => _stock.RestaurarAsync(lineasStock, input.SolicitudId, CancellationToken.None),
                error);
            throw;
        }
    }

    public async Task<Result> AnularVentaAsync(
        Guid publicId,
        string motivoAnulacion,
        CancellationToken cancellationToken = default)
    {
        var validacion = ValidarAnulacion(publicId, motivoAnulacion);
        if (!validacion.EsExito)
        {
            return validacion;
        }

        var usuario = ValidarUsuarioActual();
        if (!usuario.EsExito)
        {
            return usuario;
        }

        await using var connection = _connectionFactory.CreateConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var venta = await _ventas.ObtenerPorPublicIdAsync(publicId, connection, transaction);

        if (venta is null)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return Result.NoEncontrado("La venta especificada no existe.");
        }

        if (venta.Estado.Equals("Anulada", StringComparison.OrdinalIgnoreCase))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return Result.Conflicto("Venta", "La venta ya se encuentra anulada.");
        }

        var detalles = await _ventas.ObtenerDetallePorVentaIdAsync(
            venta.VentaId,
            connection,
            transaction);
        var lineasStock = detalles
            .Select(detalle => new StockOperacionItem(
                detalle.ProductoPublicId,
                detalle.Cantidad))
            .ToList();

        var restauracion = await _stock.RestaurarAsync(
            lineasStock,
            venta.PublicId,
            cancellationToken);
        if (!restauracion.EsExito)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return restauracion;
        }

        try
        {
            var actualizada = await _ventas.ActualizarEstadoAsync(
                venta.VentaId,
                "Anulada",
                _usuarioActual.UsuarioId,
                motivoAnulacion.Trim(),
                connection,
                transaction);

            if (!actualizada)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                var compensacion = await _stock.DescontarAsync(
                    lineasStock,
                    venta.PublicId,
                    CancellationToken.None);

                return compensacion.EsExito
                    ? Result.Conflicto("Venta", "La venta cambió mientras se intentaba anular.")
                    : compensacion;
            }

            foreach (var detalle in detalles)
            {
                await _movimientos.RegistrarAsync(
                    venta.VentaId,
                    detalle.ProductoPublicId,
                    detalle.Cantidad,
                    "Anulacion",
                    connection,
                    transaction);
            }

            await transaction.CommitAsync(cancellationToken);
            return Result.Exito();
        }
        catch (Exception error)
        {
            await IntentarRollbackAsync(transaction);
            await CompensarOPropagarAsync(
                () => _stock.DescontarAsync(lineasStock, venta.PublicId, CancellationToken.None),
                error);
            throw;
        }
    }

    public async Task<Result> CorregirDatosAsync(
        Guid publicId,
        CorregirVentaInput input,
        CancellationToken cancellationToken = default)
    {
        var validacion = ValidarCorreccion(publicId, input);
        if (!validacion.EsExito)
        {
            return validacion;
        }

        var usuario = ValidarUsuarioActual();
        if (!usuario.EsExito)
        {
            return usuario;
        }

        var existente = await _ventas.ObtenerPorPublicIdAsync(publicId);
        if (existente is null)
        {
            return Result.NoEncontrado("La venta especificada no existe.");
        }

        if (!existente.Estado.Equals("Activa", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Conflicto("Venta", "Una venta anulada no puede ser modificada.");
        }

        var cliente = await _clientes.ObtenerPorCiNitAsync(
            input.CiNitCliente,
            cancellationToken);
        if (!cliente.EsExito)
        {
            return cliente;
        }

        await using var connection = _connectionFactory.CreateConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var bloqueada = await _ventas.ObtenerPorPublicIdAsync(
                publicId,
                connection,
                transaction);

            if (bloqueada is null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return Result.NoEncontrado("La venta especificada no existe.");
            }

            if (!bloqueada.Estado.Equals("Activa", StringComparison.OrdinalIgnoreCase))
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return Result.Conflicto("Venta", "Una venta anulada no puede ser modificada.");
            }

            var observacion = string.IsNullOrWhiteSpace(input.Observacion)
                ? null
                : input.Observacion.Trim();
            var actualizada = await _ventas.ActualizarDatosAsync(
                bloqueada.VentaId,
                cliente.Valor,
                observacion,
                _usuarioActual.UsuarioId!.Value,
                connection,
                transaction);

            if (!actualizada)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return Result.Conflicto(
                    "Venta",
                    "La venta cambió mientras se intentaba corregir.");
            }

            await transaction.CommitAsync(cancellationToken);
            return Result.Exito();
        }
        catch
        {
            await IntentarRollbackAsync(transaction);
            throw;
        }
    }

    public Task<IReadOnlyList<VentaListItemModel>> ObtenerListadoAsync(
        FiltroVentasInput filtro) =>
        _ventas.ObtenerListadoAsync(
            filtro ?? new FiltroVentasInput(),
            _usuarioActual.EsAdministrador);

    public Task<VentaDetalleCompletoModel?> ObtenerDetalleCompletoAsync(Guid publicId) =>
        publicId == Guid.Empty
            ? Task.FromResult<VentaDetalleCompletoModel?>(null)
            : _ventas.ObtenerDetalleCompletoAsync(publicId, _usuarioActual.EsAdministrador);

    private async Task<VentaRegistradaResult> GuardarVentaLocalAsync(
        RegistrarVentaInput input,
        ClienteVentaSnapshot cliente,
        IReadOnlyList<DetalleVenta> detalles,
        Guid usuarioId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var total = detalles.Sum(detalle => detalle.Importe);
            var venta = new Venta
            {
                PublicId = input.SolicitudId,
                ClientePublicId = cliente.PublicId,
                ClienteCiNit = cliente.CiNit,
                ClienteRazonSocial = cliente.RazonSocial,
                Total = total,
                Estado = "Activa",
                UsuarioCreacionId = usuarioId,
                VendedorIniciales = _usuarioActual.Iniciales
            };

            var ventaId = await _ventas.InsertarAsync(venta, connection, transaction);

            foreach (var detalle in detalles)
            {
                detalle.VentaId = ventaId;
                await _ventas.InsertarDetalleAsync(detalle, connection, transaction);
                await _movimientos.RegistrarAsync(
                    ventaId,
                    detalle.ProductoPublicId,
                    detalle.Cantidad,
                    "Venta",
                    connection,
                    transaction);
            }

            await transaction.CommitAsync();
            return new VentaRegistradaResult
            {
                PublicId = venta.PublicId,
                Total = total
            };
        }
        catch
        {
            await IntentarRollbackAsync(transaction);
            throw;
        }
    }

    private async Task<Result<IReadOnlyList<DetalleVenta>>> PrepararDetallesAsync(
        IReadOnlyList<DetalleVentaInput> entradas,
        CancellationToken cancellationToken)
    {
        var detalles = new List<DetalleVenta>(entradas.Count);

        foreach (var entrada in entradas)
        {
            var producto = await _productos.ObtenerPorPublicIdAsync(
                entrada.ProductoPublicId,
                cancellationToken);
            if (!producto.EsExito)
            {
                return Result<IReadOnlyList<DetalleVenta>>.Fallo(producto);
            }

            if (producto.Valor.Stock < entrada.Cantidad)
            {
                return Result<IReadOnlyList<DetalleVenta>>.Fallo(
                    Result.Conflicto(
                        "Stock",
                        $"No existe stock suficiente de {producto.Valor.Nombre} para realizar la venta."));
            }

            var importe = entrada.Cantidad * producto.Valor.PrecioVenta;
            detalles.Add(new DetalleVenta
            {
                ProductoPublicId = producto.Valor.PublicId,
                ProductoNombre = producto.Valor.Nombre,
                Cantidad = entrada.Cantidad,
                PrecioUnitarioVenta = producto.Valor.PrecioVenta,
                CostoAdquisicionUnitario = producto.Valor.CostoAdquisicion,
                Importe = importe,
                Ganancia = entrada.Cantidad
                    * (producto.Valor.PrecioVenta - producto.Valor.CostoAdquisicion)
            });
        }

        return Result<IReadOnlyList<DetalleVenta>>.Exito(detalles);
    }

    private async Task<Result<VentaRegistradaResult?>> RecuperarVentaAsync(
        RegistrarVentaInput input,
        Guid usuarioId)
    {
        var venta = await _ventas.ObtenerPorPublicIdAsync(input.SolicitudId);
        if (venta is null)
        {
            return Result<VentaRegistradaResult?>.Exito(null);
        }

        if (!venta.ClienteCiNit.Equals(input.CiNitCliente.Trim(), StringComparison.OrdinalIgnoreCase)
            || venta.UsuarioCreacionId != usuarioId)
        {
            return Result<VentaRegistradaResult?>.Fallo(
                Result.Conflicto(
                    "SolicitudId",
                    "La solicitud de venta ya fue utilizada con datos diferentes."));
        }

        var detalles = await _ventas.ObtenerDetallePorVentaIdAsync(venta.VentaId);
        var registradas = AgruparCantidades(detalles.Select(detalle =>
            (detalle.ProductoPublicId, detalle.Cantidad)));
        var solicitadas = AgruparCantidades(input.Detalles.Select(detalle =>
            (detalle.ProductoPublicId, detalle.Cantidad)));

        if (registradas.Count != solicitadas.Count
            || registradas.Any(registro =>
                !solicitadas.TryGetValue(registro.Key, out var cantidad)
                || cantidad != registro.Value))
        {
            return Result<VentaRegistradaResult?>.Fallo(
                Result.Conflicto(
                    "SolicitudId",
                    "La solicitud de venta ya fue utilizada con productos o cantidades diferentes."));
        }

        return Result<VentaRegistradaResult?>.Exito(new VentaRegistradaResult
        {
            PublicId = venta.PublicId,
            Total = detalles.Sum(detalle => detalle.Importe)
        });
    }

    private static Dictionary<Guid, long> AgruparCantidades(
        IEnumerable<(Guid ProductoPublicId, int Cantidad)> lineas) =>
        lineas
            .GroupBy(linea => linea.ProductoPublicId)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo.Sum(linea => (long)linea.Cantidad));

    private Result ValidarUsuarioActual()
    {
        if (_usuarioActual.UsuarioId is null)
        {
            return Result.Validacion("Usuario", "No existe un usuario autenticado.");
        }

        return string.IsNullOrWhiteSpace(_usuarioActual.Iniciales)
            ? Result.Validacion("Iniciales", "La sesión no contiene las iniciales del vendedor.")
            : Result.Exito();
    }

    private static Result ValidarRegistro(RegistrarVentaInput? input)
    {
        if (input is null)
        {
            return Result.Validacion("Venta", "Debe proporcionar los datos de la venta.");
        }

        if (input.SolicitudId == Guid.Empty)
        {
            return Result.Validacion("SolicitudId", "Debe proporcionar un identificador de solicitud válido.");
        }

        if (string.IsNullOrWhiteSpace(input.CiNitCliente))
        {
            return Result.Validacion("CiNitCliente", "Debe indicar el CI/NIT del cliente.");
        }

        if (input.Detalles is null || input.Detalles.Count == 0)
        {
            return Result.Validacion("Detalles", "La venta debe contener al menos un producto.");
        }

        return input.Detalles.Any(detalle =>
                detalle is null
                || detalle.ProductoPublicId == Guid.Empty
                || detalle.Cantidad <= 0)
            ? Result.Validacion("Detalles", "La venta contiene un producto o una cantidad inválida.")
            : Result.Exito();
    }

    private static Result ValidarAnulacion(Guid publicId, string? motivoAnulacion)
    {
        if (publicId == Guid.Empty)
        {
            return Result.Validacion("PublicId", "El identificador de la venta es inválido.");
        }

        var motivo = motivoAnulacion?.Trim();
        return motivo is null || motivo.Length is < 10 or > 250
            ? Result.Validacion("Motivo", "El motivo de la anulación debe tener entre 10 y 250 caracteres.")
            : Result.Exito();
    }

    private static Result ValidarCorreccion(Guid publicId, CorregirVentaInput? input)
    {
        if (publicId == Guid.Empty)
        {
            return Result.Validacion("PublicId", "El identificador de la venta es inválido.");
        }

        if (input is null)
        {
            return Result.Validacion("Venta", "Debe proporcionar los datos a corregir.");
        }

        if (string.IsNullOrWhiteSpace(input.CiNitCliente))
        {
            return Result.Validacion("CiNitCliente", "Debe indicar el CI/NIT del cliente.");
        }

        return input.Observacion?.Trim().Length > 250
            ? Result.Validacion("Observacion", "La observación no puede superar los 250 caracteres.")
            : Result.Exito();
    }

    private static async Task CompensarOPropagarAsync(
        Func<Task<Result>> compensar,
        Exception errorOriginal)
    {
        var resultado = await compensar();
        if (!resultado.EsExito)
        {
            throw new AggregateException(
                errorOriginal,
                new InvalidOperationException(resultado.Errores.Values.First()));
        }
    }

    private static async Task IntentarRollbackAsync(DbTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // La transacción ya pudo cerrarse al fallar el commit; se conserva el error original.
        }
    }
}
