using Microsoft.AspNetCore.DataProtection;

namespace Libreria.Client.Seguridad;

public sealed class UrlProtector : IUrlProtector
{
    private readonly IDataProtector _protector;

    public UrlProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Libreria.Seguridad.URLs.v1");
    }

    public string Cifrar(Guid publicId) => _protector.Protect(publicId.ToString());

    public Guid? Descifrar(string textoCifrado)
    {
        try
        {
            var descifrado = _protector.Unprotect(textoCifrado);
            return Guid.TryParse(descifrado, out var publicId) ? publicId : null;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }
}
