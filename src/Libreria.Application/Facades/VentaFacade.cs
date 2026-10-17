using System.Data;
using System.Data.Common;
using Libreria.Domain;
using Libreria.Application.Factories;
using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Application.Ports.Secondary;

namespace Libreria.Application.Facades
{
    public class VentaFacade : IVentaFacade
    {
        private readonly IVentaRepository _ventaRepository;
        private readonly IProductoRepository _productoRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IUsuarioActual _usuarioActual;
        private readonly IStockFacade _stockFacade;

        public VentaFacade(
            IVentaRepository ventaRepository,
            IProductoRepository productoRepository,
            IClienteRepository clienteRepository,
            IDbConnectionFactory connectionFactory,
            IUsuarioActual usuarioActual,
            IStockFacade stockFacade)
        {
            _ventaRepository = ventaRepository;
            _productoRepository = productoRepository;
            _clienteRepository = clienteRepository;
            _connectionFactory = connectionFactory;
            _usuarioActual = usuarioActual;
            _stockFacade = stockFacade;
        }

        public async Task<VentaRegistradaResult> RegistrarVentaAsync(
            RegistrarVentaInput input)
        {
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
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

            var usuarioId = _usuarioActual.UsuarioId
                ?? throw new VentaRegistroException(
                    "No existe un usuario autenticado.");

            var cliente = await _clienteRepository.ObtenerPorCiNitAsync(
                input.CiNitCliente);

            if (cliente is null)
            {
                throw new VentaRegistroException(
                    "El cliente seleccionado no existe.");
            }

            foreach (var detalle in input.Detalles)
            {
                if (detalle is null || detalle.ProductoPublicId == Guid.Empty)
                {
                    throw new ArgumentException(
                        "La venta contiene un producto inválido.",
                        nameof(input));
                }

                if (detalle.Cantidad <= 0)
                {
                    throw new ArgumentException(
                        "La cantidad de cada producto debe ser mayor a cero.",
                        nameof(input));
                }
            }

            if (input.SolicitudId != Guid.Empty)
            {
                var registrada = await RecuperarVentaAsync(input, cliente.ClienteId, usuarioId);
                if (registrada is not null)
                {
                    return registrada;
                }
            }

            await using var connection = _connectionFactory.CreateConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var detallesPreparados = new List<DetalleVenta>();
                decimal total = 0m;

                foreach (var detalleInput in input.Detalles)
                {
                    var producto = await _productoRepository.ObtenerParaVentaAsync(
                        detalleInput.ProductoPublicId,
                        connection,
                        transaction);

                    if (producto is null)
                    {
                        throw new VentaRegistroException(
                            "Uno de los productos seleccionados no existe o está inactivo.");
                    }

                    if (producto.Stock < detalleInput.Cantidad)
                    {
                        throw new VentaRegistroException(
                            $"No existe stock suficiente de {producto.Nombre} para realizar la venta.");
                    }

                    var importe =
                        detalleInput.Cantidad * producto.PrecioVenta;

                    var ganancia =
                        detalleInput.Cantidad *
                        (producto.PrecioVenta - producto.CostoAdquisicionActual);

                    total += importe;

                    detallesPreparados.Add(new DetalleVenta
                    {
                        ProductoId = producto.ProductoId,
                        Cantidad = detalleInput.Cantidad,
                        PrecioUnitarioVenta = producto.PrecioVenta,
                        CostoAdquisicionUnitario = producto.CostoAdquisicionActual,
                        Importe = importe,
                        Ganancia = ganancia
                    });
                }

                var venta = new Venta
                {
                    PublicId = input.SolicitudId == Guid.Empty
                        ? Guid.NewGuid()
                        : input.SolicitudId,
                    ClienteId = cliente.ClienteId,
                    Total = total,
                    Estado = "Activa",
                    UsuarioCreacionId = usuarioId
                };

                var ventaId = await _ventaRepository.InsertarAsync(
                    venta,
                    connection,
                    transaction);

                foreach (var detalleVenta in detallesPreparados)
                {
                    detalleVenta.VentaId = ventaId;

                    await _ventaRepository.InsertarDetalleAsync(
                        detalleVenta,
                        connection,
                        transaction);

                    try
                    {
                        await _stockFacade.DescontarStockAsync(
                            detalleVenta.ProductoId,
                            detalleVenta.Cantidad,
                            connection,
                            transaction);
                    }
                    catch (InvalidOperationException errorStock)
                    {
                        throw new VentaRegistroException(
                            "No existe stock suficiente para realizar la venta.",
                            innerException: errorStock);
                    }
                }
                await transaction.CommitAsync();

                return new VentaRegistradaResult
                {
                    PublicId = venta.PublicId,
                    Total = total
                };
            }
            catch (Exception error)
            {
                var rollbackCompletado = false;
                Exception errorOriginal = error;
                try
                {
                    await transaction.RollbackAsync();
                    rollbackCompletado = true;
                }
                catch (Exception errorRollback)
                {
                    errorOriginal = new AggregateException(error, errorRollback);
                }

                if (input.SolicitudId != Guid.Empty && (error is DbException || !rollbackCompletado))
                {
                    try
                    {
                        var registrada = await RecuperarVentaAsync(input, cliente.ClienteId, usuarioId);
                        if (registrada is not null)
                        {
                            return registrada;
                        }
                    }
                    catch (VentaRegistroException)
                    {
                        throw;
                    }
                    catch (Exception errorConsulta)
                    {
                        errorOriginal = new AggregateException(errorOriginal, errorConsulta);
                    }
                }

                if (rollbackCompletado && error is VentaRegistroException)
                {
                    throw;
                }

                throw new VentaRegistroException(
                    rollbackCompletado
                        ? "No se pudo guardar la venta. Intente nuevamente."
                        : "No se pudo confirmar el resultado de la venta. Reintente la misma solicitud para comprobarla sin duplicarla.",
                    resultadoIncierto: !rollbackCompletado,
                    innerException: errorOriginal);
            }
        }

        private async Task<VentaRegistradaResult?> RecuperarVentaAsync(
            RegistrarVentaInput input,
            int clienteId,
            int usuarioId)
        {
            var venta = await _ventaRepository.ObtenerPorPublicIdAsync(input.SolicitudId);
            if (venta is null)
            {
                return null;
            }

            if (venta.ClienteId != clienteId || venta.UsuarioCreacionId != usuarioId)
            {
                throw new VentaRegistroException("La solicitud de venta ya fue utilizada con datos diferentes.");
            }

            var detalles = await _ventaRepository.ObtenerDetallePorVentaIdAsync(venta.VentaId);
            var cantidadesRegistradas = detalles
                .GroupBy(d => d.ProductoId)
                .ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(d => (long)d.Cantidad));
            var solicitados = input.Detalles.GroupBy(d => d.ProductoPublicId).ToList();

            if (cantidadesRegistradas.Count != solicitados.Count)
            {
                throw new VentaRegistroException("La solicitud de venta ya fue utilizada con productos diferentes.");
            }

            foreach (var grupo in solicitados)
            {
                var producto = await _productoRepository.ObtenerPorPublicIdAsync(grupo.Key, true)
                    ?? await _productoRepository.ObtenerPorPublicIdAsync(grupo.Key, false);
                if (producto is null ||
                    !cantidadesRegistradas.TryGetValue(producto.ProductoId, out var cantidad) ||
                    cantidad != grupo.Sum(d => (long)d.Cantidad))
                {
                    throw new VentaRegistroException("La solicitud de venta ya fue utilizada con productos o cantidades diferentes.");
                }
            }

            return new VentaRegistradaResult
            {
                PublicId = venta.PublicId,
                Total = detalles.Sum(d => d.Importe)
            };
        }

        public async Task AnularVentaAsync(Guid publicId)
        {
            if (publicId == Guid.Empty)
            {
                throw new ArgumentException("El identificador de la venta es inválido.", nameof(publicId));
            }

            var usuarioId = _usuarioActual.UsuarioId
                ?? throw new InvalidOperationException("No existe un usuario autenticado.");

            await using var connection = _connectionFactory.CreateConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var venta = await _ventaRepository.ObtenerPorPublicIdAsync(publicId, connection, transaction);
                if (venta is null)
                {
                    throw new KeyNotFoundException("La venta especificada no existe.");
                }

                if (string.Equals(venta.Estado, "Anulada", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("La venta ya se encuentra anulada.");
                }

                var lineas = await _ventaRepository.ObtenerDetallePorVentaIdAsync(venta.VentaId, connection, transaction);

                var actualizado = await _ventaRepository.ActualizarEstadoAsync(
                    venta.VentaId,
                    "Anulada",
                    usuarioId,
                    connection,
                    transaction);

                if (!actualizado)
                {
                    throw new InvalidOperationException("No se pudo actualizar el estado de la venta.");
                }

                foreach (var detalle in lineas)
                {
                    await _stockFacade.RestaurarStockAsync(
                        detalle.ProductoId,
                        detalle.Cantidad,
                        connection,
                        transaction);
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IReadOnlyList<VentaListItemModel>> ObtenerListadoAsync(
            FiltroVentasInput filtro)
        {
            filtro ??= new FiltroVentasInput();
            return await _ventaRepository.ObtenerListadoAsync(filtro);
        }

        public async Task<VentaDetalleCompletoModel?> ObtenerDetalleCompletoAsync(
            Guid publicId,
            bool esAdministrador)
        {
            if (publicId == Guid.Empty)
            {
                return null;
            }

            return await _ventaRepository.ObtenerDetalleCompletoAsync(publicId, esAdministrador);
        }

        public void AnularVenta() => throw new NotSupportedException("Utilice AnularVentaAsync.");
    }
}
