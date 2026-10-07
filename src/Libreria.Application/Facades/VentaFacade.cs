using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
using Libreria.Application.Ports.Primary;

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
            await using var transaction = await connection.BeginTransactionAsync();

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
        public void AnularVenta() => throw new System.NotImplementedException();
    }
}
