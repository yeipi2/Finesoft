namespace fs_backend.Repositories;

public interface IAuthCheckService
{
    /// <summary>
    /// Devuelve true si el email está disponible (no existe en el sistema).
    /// excludeUserId: pasar el UserId actual cuando se edita, para no bloquearse a sí mismo.
    /// </summary>
    Task<bool> IsEmailAvailableAsync(string email, string? excludeUserId = null);
}