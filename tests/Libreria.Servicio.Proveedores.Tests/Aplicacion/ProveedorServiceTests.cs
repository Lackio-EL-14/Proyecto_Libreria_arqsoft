using Libreria.Servicio.Proveedores.Application.Models;
using Libreria.Servicio.Proveedores.Application.Services;
using Libreria.Servicio.Proveedores.Application.Validators;
using Libreria.Servicio.Proveedores.Domain;

namespace Libreria.Servicio.Proveedores.Tests.Aplicacion;

/// <summary>US-118: alta, edición, baja lógica y reactivación por el puerto primario.</summary>
public sealed class ProveedorServiceTests
{
    private readonly RepositorioProveedoresEnMemoria _repositorio = new();

    private ProveedorService Servicio()
    {
        var fabrica = new FabricaEnMemoria(_repositorio);
        return new ProveedorService(fabrica, new ProveedorValidator(fabrica));
    }

    [Fact]
    public async Task Registrar_guarda_el_proveedor_con_su_contacto_principal()
    {
        var resultado = await Servicio().RegistrarAsync(ProveedorValidatorTests.Valido());

        Assert.True(resultado.IsSuccess);
        Assert.Equal("José Mamani", resultado.Valor.NombreContacto);
        Assert.True(resultado.Valor.Estado);
        var guardado = Assert.Single(_repositorio.Proveedores);
        Assert.True(guardado.ContactoPrincipal!.EsPrincipal);
    }

    [Fact]
    public async Task Registrar_con_datos_invalidos_no_guarda_nada()
    {
        var resultado = await Servicio().RegistrarAsync(new ProveedorInput());

        Assert.Equal(TipoError.Validacion, resultado.Tipo);
        Assert.Empty(_repositorio.Proveedores);
    }

    [Fact]
    public async Task Duplicado_detectado_por_la_base_se_traduce_a_conflicto()
    {
        _repositorio.LanzarDuplicadoAlGuardar = true;

        var resultado = await Servicio().RegistrarAsync(ProveedorValidatorTests.Valido());

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Equal(ProveedorValidator.MensajeNitDuplicado, Assert.Single(resultado.Errors()));
    }

    [Fact]
    public async Task Actualizar_un_proveedor_inexistente_es_no_encontrado()
    {
        var resultado = await Servicio().ActualizarAsync(Guid.NewGuid(), ProveedorValidatorTests.Valido());

        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
    }

    [Fact]
    public async Task Actualizar_un_proveedor_inactivo_es_conflicto()
    {
        var publicId = Guid.NewGuid();
        _repositorio.Proveedores.Add(new Proveedor { PublicId = publicId, Nit = "1023456019", Estado = false });

        var resultado = await Servicio().ActualizarAsync(publicId, ProveedorValidatorTests.Valido());

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Equal("El proveedor está inactivo. Reactívelo antes de editarlo.", Assert.Single(resultado.Errors()));
    }

    [Fact]
    public async Task Actualizar_aplica_los_cambios()
    {
        var publicId = Guid.NewGuid();
        _repositorio.Proveedores.Add(new Proveedor { PublicId = publicId, Nit = "1023456019", RazonSocial = "Antigua" });
        var input = ProveedorValidatorTests.Valido();
        input.RazonSocial = "Distribuidora Andina SRL";

        var resultado = await Servicio().ActualizarAsync(publicId, input);

        Assert.True(resultado.IsSuccess);
        Assert.Equal("Distribuidora Andina SRL", resultado.Valor.RazonSocial);
    }

    [Fact]
    public async Task Baja_logica_y_reactivacion()
    {
        var publicId = Guid.NewGuid();
        _repositorio.Proveedores.Add(new Proveedor { PublicId = publicId, Nit = "1", Estado = true });
        var servicio = Servicio();

        Assert.True((await servicio.CambiarEstadoAsync(publicId, false)).IsSuccess);
        Assert.False(_repositorio.Proveedores[0].Estado);
        Assert.Equal(TipoError.NoEncontrado, (await servicio.CambiarEstadoAsync(publicId, false)).Tipo);
        Assert.True((await servicio.CambiarEstadoAsync(publicId, true)).IsSuccess);
    }

    [Fact]
    public async Task Listar_filtra_por_estado()
    {
        _repositorio.Proveedores.Add(new Proveedor { PublicId = Guid.NewGuid(), RazonSocial = "Activo", Estado = true });
        _repositorio.Proveedores.Add(new Proveedor { PublicId = Guid.NewGuid(), RazonSocial = "Inactivo", Estado = false });

        var activos = await Servicio().ListarAsync(new FiltroProveedores());

        Assert.Equal("Activo", Assert.Single(activos).RazonSocial);
    }
}
