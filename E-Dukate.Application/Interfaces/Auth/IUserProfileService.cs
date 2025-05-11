namespace E_Dukate.Application.Interfaces.Auth
{
    public interface IUserProfileService
    {
        Task<string> GetFullNameAsync(Guid userId, string role);
    }
}