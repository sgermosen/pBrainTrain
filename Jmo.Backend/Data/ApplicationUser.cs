using Microsoft.AspNetCore.Identity;

namespace Jmo.Backend.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string Apodo { get; set; }
        public string Nombre { get; set; }
    }
}
