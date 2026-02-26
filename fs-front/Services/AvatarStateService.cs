// Services/AvatarStateService.cs
namespace fs_front.Services;

/// <summary>
/// Servicio singleton para notificar a NavBar cuando cambia el avatar,
/// sin depender de localStorage ni JS.
/// </summary>
public static class AvatarStateService
{
    public static event Action<string?>? OnAvatarChanged;

    public static void NotifyAvatarChanged(string? dataUrl)
    {
        OnAvatarChanged?.Invoke(dataUrl);
    }
}