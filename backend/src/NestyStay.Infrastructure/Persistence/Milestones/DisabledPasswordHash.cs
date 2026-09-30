using System.Security.Cryptography;

namespace NestyStay.Infrastructure.Persistence.Milestones;

/// <summary>
/// Creates a syntactically valid but unusable password hash for synthetic
/// service identities. These identities are disabled and must never receive a
/// known password or a reusable credential.
/// </summary>
internal static class DisabledPasswordHash
{
    private const int Iterations = 120_000;

    public static string Create()
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = RandomNumberGenerator.GetBytes(32);
        return $"PBKDF2-SHA256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
}
