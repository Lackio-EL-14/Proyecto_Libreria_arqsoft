using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
using Libreria.Application.Ports.Primary;
using System.Data.Common;

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

        // Inyección de dependencia: La fachada principal orquesta a la secundaria
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
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var venta = new Venta
                {
                    PublicId = input.SolicitudId == Guid.Empty
                        ? Guid.NewGuid()
                        : input.SolicitudId,
                    ClienteId = cliente.ClienteId,
                    Estado = "Activa",
                    UsuarioCreacionId = usuarioId
                };

                var ventaId = await _ventaRepository.InsertarAsync(
                    venta,
                    connection,
                    transaction);
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

                    total += importe;

                    var ganancia =
                        detalleInput.Cantidad *
                        (producto.PrecioVenta - producto.CostoAdquisicionActual);

                    var detalleVenta = new DetalleVenta
                    {
                        VentaId = ventaId,
                        ProductoId = producto.ProductoId,
                        Cantidad = detalleInput.Cantidad,
                        PrecioUnitarioVenta = producto.PrecioVenta,
                        CostoAdquisicionUnitario = producto.CostoAdquisicionActual,
                        Importe = importe,
                        Ganancia = ganancia
                    };

                    await _ventaRepository.InsertarDetalleAsync(
                        detalleVenta,
                        connection,
                        transaction);
                    try
                    {
                        await _stockFacade.DescontarStockAsync(
                            producto.ProductoId,
                            detalleInput.Cantidad,
                            connection,
                            transaction);
                    }
                    catch (InvalidOperationException errorStock)
                    {
                        throw new VentaRegistroException(
                            $"No existe stock suficiente de {producto.Nombre} para realizar la venta.",
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
                    // Conservar ambos errores si se pierde la conexión durante el rollback.
                    errorOriginal = new AggregateException(error, errorRollback);
                }

                if (input.SolicitudId != Guid.Empty && (error is DbException || !rollbackCompletado))
                {
                    try
                    {
                        // Otra conexión permite recuperar una venta ya confirmada aunque se perdiera su respuesta.
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
                // Un reintento también debe recuperar productos que se hayan dado de baja después de la venta.
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
        public void AnularVenta() => throw new System.NotImplementedException();
    }
}
