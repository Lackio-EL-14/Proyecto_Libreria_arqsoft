using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
using Libreria.Application.Ports.Primary;
using System.Data;
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

            if (input.Detalles.Count == 0)
            {
                throw new ArgumentException(
                    "La venta debe contener al menos un producto.",
                    nameof(input));
            }

            var usuarioId = _usuarioActual.UsuarioId
                ?? throw new InvalidOperationException(
                    "No existe un usuario autenticado.");

            var cliente = await _clienteRepository.ObtenerPorCiNitAsync(
                input.CiNitCliente);

            if (cliente is null)
            {
                throw new InvalidOperationException(
                    "El cliente seleccionado no existe.");
            }

            foreach (var detalle in input.Detalles)
            {
                if (detalle.ProductoPublicId == Guid.Empty)
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

            await using var connection = _connectionFactory.CreateConnection();
            if (connection.State != ConnectionState.Open)
            {
                await ((DbConnection)connection).OpenAsync();
            }

            await using var transaction = await ((DbConnection)connection).BeginTransactionAsync();

            try
            {
                var venta = new Venta
                {
                    PublicId = Guid.NewGuid(),
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
                        throw new InvalidOperationException(
                            "Uno de los productos seleccionados no existe o está inactivo.");
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
                    await _stockFacade.DescontarStockAsync(
                        producto.ProductoId,
                        detalleInput.Cantidad,
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
                await ((DbConnection)connection).OpenAsync();
            }

            await using var transaction = await ((DbConnection)connection).BeginTransactionAsync();

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
    }
}
