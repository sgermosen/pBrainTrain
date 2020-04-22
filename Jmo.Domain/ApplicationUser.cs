using Microsoft.AspNetCore.Identity;

namespace Jmo.Web.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string Nickname { get; set; }
        public string Firtsname { get; set; }
        public string Lastname { get; set; }
    }
}
