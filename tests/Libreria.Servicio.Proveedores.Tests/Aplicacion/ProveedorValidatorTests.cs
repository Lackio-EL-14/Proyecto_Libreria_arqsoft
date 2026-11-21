using Libreria.Servicio.Proveedores.Application.Models;
using Libreria.Servicio.Proveedores.Application.Validators;
using Libreria.Servicio.Proveedores.Domain;

namespace Libreria.Servicio.Proveedores.Tests.Aplicacion;

/// <summary>US-117: validaciones del proveedor devueltas como Result (sin excepciones).</summary>
public sealed class ProveedorValidatorTests
{
    private readonly RepositorioProveedoresEnMemoria _repositorio = new();
    private ProveedorValidator Validador => new(new FabricaEnMemoria(_repositorio));

    internal static ProveedorInput Valido() => new()
    {
        Nit = "1023456019",
        RazonSocial = "Distribuidora Andina S.R.L.",
        Correo = "ventas@andina.bo",
        NombreContacto = "José Mamani",
        TelefonoContacto = "70000000"
    };

    [Fact]
    public async Task Datos_validos_no_tienen_errores()
    {
        Assert.True((await Validador.ValidarAsync(Valido())).IsSuccess);
    }

    [Fact]
    public async Task Campos_obligatorios_vacios_devuelven_un_mensaje_por_campo_con_concordancia()
    {
        var resultado = await Validador.ValidarAsync(new ProveedorInput());

        Assert.True(resultado.IsError);
        Assert.Equal(TipoError.Validacion, resultado.Tipo);
        Assert.Equal("El NIT es obligatorio.", resultado.Errores[nameof(ProveedorInput.Nit)]);
        Assert.Equal("La razón social es obligatoria.", resultado.Errores[nameof(ProveedorInput.RazonSocial)]);
        Assert.Equal("El nombre del contacto es obligatorio.", resultado.Errores[nameof(ProveedorInput.NombreContacto)]);
        Assert.Equal("Ingrese al menos un teléfono o correo para el contacto principal.", resultado.Errores[nameof(ProveedorInput.TelefonoContacto)]);
    }

    [Fact]
    public async Task Correo_invalido_se_rechaza()
    {
        var input = Valido();
        input.Correo = "ventas@";

        var resultado = await Validador.ValidarAsync(input);

        Assert.Equal("El correo del proveedor no es válido.", resultado.Errores[nameof(ProveedorInput.Correo)]);
    }

    [Fact]
    public async Task Nit_repetido_es_conflicto()
    {
        _repositorio.Proveedores.Add(new Proveedor { PublicId = Guid.NewGuid(), Nit = "1023456019" });

        var resultado = await Validador.ValidarAsync(Valido());

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Equal(ProveedorValidator.MensajeNitDuplicado, Assert.Single(resultado.Errors()));
    }

    [Fact]
    public async Task El_mismo_proveedor_puede_conservar_su_nit_al_editar()
    {
        var publicId = Guid.NewGuid();
        _repositorio.Proveedores.Add(new Proveedor { PublicId = publicId, Nit = "1023456019" });

        Assert.True((await Validador.ValidarAsync(Valido(), publicId)).IsSuccess);
    }

    [Fact]
    public void Normalizar_quita_espacios_sobrantes_y_conserva_nombres_compuestos()
    {
        var input = Valido();
        input.RazonSocial = "  Distribuidora   Andina  ";
        input.NombreContacto = " Juan   José  Pérez ";
        input.Telefono = "   ";

        Validador.Normalizar(input);

        Assert.Equal("Distribuidora Andina", input.RazonSocial);
        Assert.Equal("Juan José Pérez", input.NombreContacto);
        Assert.Null(input.Telefono);
    }
}
