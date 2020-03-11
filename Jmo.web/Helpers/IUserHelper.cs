using Jmo.Web.Data;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace Jmo.Web.Helpers
{
    public interface IUserHelper
    {
        Task<ApplicationUser> GetUserByEmailAsync(string email);

        Task<IdentityResult> AddUserAsync(ApplicationUser user, string password);
    }

}
