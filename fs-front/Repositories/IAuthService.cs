using fs_front.Models;

namespace fs_front.Services
{
    public interface IAuthService
    {
        Task<FormResponse> LoginAsync(LoginModel loginModel);
        Task<FormResponse> RegisterAsync(RegisterModel register);
        void Logout();
    }
}
