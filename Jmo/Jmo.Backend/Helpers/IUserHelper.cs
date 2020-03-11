using Jmo.Backend.Data;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace Jmo.Backend.Helpers
{
    public interface IUserHelper
    {
        Task<ApplicationUser> GetUserByEmailAsync(string email);

        Task<IdentityResult> AddUserAsync(ApplicationUser user, string password);
    }

}
