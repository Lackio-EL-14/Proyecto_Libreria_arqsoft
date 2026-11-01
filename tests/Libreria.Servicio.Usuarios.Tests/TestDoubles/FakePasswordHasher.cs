using Libreria.Servicio.Usuarios.Application.Abstractions;

namespace Libreria.Servicio.Usuarios.Tests.TestDoubles;

public sealed class FakePasswordHasher : IPasswordHasher
{
    public bool VerifyResult { get; set; } = true;

    public bool RequiresRehashResult { get; set; }

    public string GeneratedHash { get; set; } = "nuevo-hash";

    public string Hash(string password)
    {
        return GeneratedHash;
    }

    public bool Verify(string hash, string password)
    {
        return VerifyResult;
    }

    public bool RequiresRehash(string hash)
    {
        return RequiresRehashResult;
    }
}