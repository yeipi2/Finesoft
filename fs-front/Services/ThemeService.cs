using Blazored.LocalStorage;
using Microsoft.JSInterop;

namespace fs_front.Services;

public class ThemeService
{
    private readonly ILocalStorageService _localStorage;
    private readonly IJSRuntime _jsRuntime;
    private bool _isDarkMode = false;

    public event Action? OnThemeChanged;

    public ThemeService(ILocalStorageService localStorage, IJSRuntime jsRuntime)
    {
        _localStorage = localStorage;
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _isDarkMode = await _localStorage.GetItemAsync<bool>("isDarkMode");
            await ApplyThemeAsync();
        }
        catch
        {
            _isDarkMode = false;
        }
    }

    public async Task ToggleThemeAsync()
    {
        _isDarkMode = !_isDarkMode;
        await _localStorage.SetItemAsync("isDarkMode", _isDarkMode);
        await ApplyThemeAsync();
        OnThemeChanged?.Invoke();
    }

    private async Task ApplyThemeAsync()
    {
        await _jsRuntime.InvokeVoidAsync("eval",
            $"document.documentElement.setAttribute('data-theme', '{(_isDarkMode ? "dark" : "light")}')");
    }

    public bool IsDarkMode => _isDarkMode;
}