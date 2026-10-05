using System;
using Microsoft.AspNetCore.DataProtection;
using Libreria.Application.Ports.Primary;

namespace Libreria.Infrastructure.Security
{
    public class UrlProtector : IUrlProtector
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
                return Guid.Parse(descifrado);
            }
            catch
            {
                return null; // Si fue manipulado o corrupto, retorna null
            }
        }
    }
}
