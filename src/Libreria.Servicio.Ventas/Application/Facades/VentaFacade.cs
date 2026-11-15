using System.Data;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Primary;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Domain;

namespace Libreria.Servicio.Ventas.Application.Facades;

public class VentaFacade : IVentaFacade
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

    public async Task<VentaRegistradaResult> RegistrarVentaAsync(
        RegistrarVentaInput input)
    {
        ValidarRegistro(input);

        var usuarioId = _usuarioActual.UsuarioId
            ?? throw new VentaRegistroException(
                "No existe un usuario autenticado.");

        var cliente = await _clientes.ObtenerPorCiNitAsync(input.CiNitCliente);
        if (cliente is null)
        {
            throw new VentaRegistroException(
                "El cliente seleccionado no existe o está inactivo.");
        }

        var registrada = await RecuperarVentaAsync(input, cliente.PublicId, usuarioId);
        if (registrada is not null)
        {
            return registrada;
        }

        var detalles = await PrepararDetallesAsync(input.Detalles);
        var total = detalles.Sum(detalle => detalle.Importe);
        var stockDescontado = new List<DetalleVenta>();

        try
        {
            foreach (var detalle in detalles)
            {
                await _stock.DescontarStockAsync(
                    detalle.ProductoPublicId,
                    detalle.Cantidad,
                    input.SolicitudId);
                stockDescontado.Add(detalle);
            }

            await using var connection = _connectionFactory.CreateConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
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

                var ventaId = await _ventas.InsertarAsync(
                    venta,
                    connection,
                    transaction);

                foreach (var detalle in detalles)
                {
                    detalle.VentaId = ventaId;
                    await _ventas.InsertarDetalleAsync(
                        detalle,
                        connection,
                        transaction);
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
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception error)
        {
            var errorFinal = await RestaurarStockDescontadoAsync(
                stockDescontado,
                input.SolicitudId,
                error);

            if (errorFinal == error && error is VentaRegistroException)
            {
                throw;
            }

            throw new VentaRegistroException(
                errorFinal == error
                    ? "No se pudo guardar la venta. Intente nuevamente."
                    : "No se pudo guardar la venta ni restaurar todo el stock.",
                resultadoIncierto: errorFinal != error,
                innerException: errorFinal);
        }
    }

    public async Task AnularVentaAsync(Guid publicId, string motivoAnulacion)
    {
        ValidarAnulacion(publicId, motivoAnulacion);

        var usuarioId = _usuarioActual.UsuarioId
            ?? throw new InvalidOperationException(
                "No existe un usuario autenticado.");

        await using var connection = _connectionFactory.CreateConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var transaction = await connection.BeginTransactionAsync();
        var stockRestaurado = new List<DetalleVenta>();

        try
        {
            var venta = await _ventas.ObtenerPorPublicIdAsync(
                publicId,
                connection,
                transaction);

            if (venta is null)
            {
                throw new KeyNotFoundException(
                    "La venta especificada no existe.");
            }

            if (venta.Estado.Equals("Anulada", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "La venta ya se encuentra anulada.");
            }

            var detalles = await _ventas.ObtenerDetallePorVentaIdAsync(
                venta.VentaId,
                connection,
                transaction);

            foreach (var detalle in detalles)
            {
                await _stock.RestaurarStockAsync(
                    detalle.ProductoPublicId,
                    detalle.Cantidad,
                    venta.PublicId);
                stockRestaurado.Add(detalle);
            }

            var actualizada = await _ventas.ActualizarEstadoAsync(
                venta.VentaId,
                "Anulada",
                usuarioId,
                motivoAnulacion.Trim(),
                connection,
                transaction);

            if (!actualizada)
            {
                throw new InvalidOperationException(
                    "No se pudo actualizar el estado de la venta.");
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

            await transaction.CommitAsync();
        }
        catch (Exception error)
        {
            await transaction.RollbackAsync();

            foreach (var detalle in stockRestaurado.AsEnumerable().Reverse())
            {
                try
                {
                    await _stock.DescontarStockAsync(
                        detalle.ProductoPublicId,
                        detalle.Cantidad,
                        publicId);
                }
                catch (Exception errorCompensacion)
                {
                    throw new AggregateException(error, errorCompensacion);
                }
            }

            throw;
        }
    }

    public Task<IReadOnlyList<VentaListItemModel>> ObtenerListadoAsync(
        FiltroVentasInput filtro) =>
        _ventas.ObtenerListadoAsync(filtro ?? new FiltroVentasInput());

    public Task<VentaDetalleCompletoModel?> ObtenerDetalleCompletoAsync(
        Guid publicId,
        bool esAdministrador) =>
        publicId == Guid.Empty
            ? Task.FromResult<VentaDetalleCompletoModel?>(null)
            : _ventas.ObtenerDetalleCompletoAsync(publicId, esAdministrador);

    private async Task<IReadOnlyList<DetalleVenta>> PrepararDetallesAsync(
        IReadOnlyList<DetalleVentaInput> entradas)
    {
        var detalles = new List<DetalleVenta>(entradas.Count);

        foreach (var entrada in entradas)
        {
            var producto = await _productos.ObtenerPorPublicIdAsync(
                entrada.ProductoPublicId);

            if (producto is null)
            {
                throw new VentaRegistroException(
                    "Uno de los productos seleccionados no existe o está inactivo.");
            }

            if (producto.Stock < entrada.Cantidad)
            {
                throw new VentaRegistroException(
                    $"No existe stock suficiente de {producto.Nombre} para realizar la venta.");
            }

            var importe = entrada.Cantidad * producto.PrecioVenta;
            detalles.Add(new DetalleVenta
            {
                ProductoPublicId = producto.PublicId,
                ProductoNombre = producto.Nombre,
                Cantidad = entrada.Cantidad,
                PrecioUnitarioVenta = producto.PrecioVenta,
                CostoAdquisicionUnitario = producto.CostoAdquisicion,
                Importe = importe,
                Ganancia = entrada.Cantidad
                    * (producto.PrecioVenta - producto.CostoAdquisicion)
            });
        }

        return detalles;
    }

    private async Task<VentaRegistradaResult?> RecuperarVentaAsync(
        RegistrarVentaInput input,
        Guid clientePublicId,
        Guid usuarioId)
    {
        var venta = await _ventas.ObtenerPorPublicIdAsync(input.SolicitudId);
        if (venta is null)
        {
            return null;
        }

        if (venta.ClientePublicId != clientePublicId
            || venta.UsuarioCreacionId != usuarioId)
        {
            throw new VentaRegistroException(
                "La solicitud de venta ya fue utilizada con datos diferentes.");
        }

        var detalles = await _ventas.ObtenerDetallePorVentaIdAsync(venta.VentaId);
        var registradas = detalles
            .GroupBy(detalle => detalle.ProductoPublicId)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo.Sum(detalle => (long)detalle.Cantidad));
        var solicitadas = input.Detalles
            .GroupBy(detalle => detalle.ProductoPublicId)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo.Sum(detalle => (long)detalle.Cantidad));

        if (registradas.Count != solicitadas.Count
            || registradas.Any(registro =>
                !solicitadas.TryGetValue(registro.Key, out var cantidad)
                || cantidad != registro.Value))
        {
            throw new VentaRegistroException(
                "La solicitud de venta ya fue utilizada con productos o cantidades diferentes.");
        }

        return new VentaRegistradaResult
        {
            PublicId = venta.PublicId,
            Total = detalles.Sum(detalle => detalle.Importe)
        };
    }

    private async Task<Exception> RestaurarStockDescontadoAsync(
        IReadOnlyList<DetalleVenta> detalles,
        Guid ventaPublicId,
        Exception errorOriginal)
    {
        Exception error = errorOriginal;

        foreach (var detalle in detalles.Reverse())
        {
            try
            {
                await _stock.RestaurarStockAsync(
                    detalle.ProductoPublicId,
                    detalle.Cantidad,
                    ventaPublicId);
            }
            catch (Exception errorCompensacion)
            {
                error = new AggregateException(error, errorCompensacion);
            }
        }

        return error;
    }

    private static void ValidarRegistro(RegistrarVentaInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.SolicitudId == Guid.Empty)
        {
            throw new ArgumentException(
                "Debe proporcionar un identificador de solicitud válido.",
                nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.CiNitCliente))
        {
            throw new ArgumentException(
                "Debe indicar el CI/NIT del cliente.",
                nameof(input));
        }

        if (input.Detalles is null || input.Detalles.Count == 0)
        {
            throw new ArgumentException(
                "La venta debe contener al menos un producto.",
                nameof(input));
        }

        if (input.Detalles.Any(detalle =>
                detalle is null
                || detalle.ProductoPublicId == Guid.Empty
                || detalle.Cantidad <= 0))
        {
            throw new ArgumentException(
                "La venta contiene un producto o una cantidad inválida.",
                nameof(input));
        }
    }

    private static void ValidarAnulacion(
        Guid publicId,
        string motivoAnulacion)
    {
        if (publicId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la venta es inválido.",
                nameof(publicId));
        }

        var motivo = motivoAnulacion?.Trim();
        if (motivo is null || motivo.Length is < 10 or > 250)
        {
            throw new ArgumentException(
                "El motivo de la anulación debe tener entre 10 y 250 caracteres.",
                nameof(motivoAnulacion));
        }
    }
}
