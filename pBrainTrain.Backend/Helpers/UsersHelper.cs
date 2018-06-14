 namespace pBrainTrain.Backend.Helpers
{
    using pBrainTrain.Backend.Models;
    using Microsoft.AspNet.Identity;
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Threading.Tasks;
    using System.Web.Configuration;
    using Microsoft.AspNet.Identity.EntityFramework;
    using System.Linq;
    using Domain;

    public class UsersHelper : IDisposable
    {
        private static readonly ApplicationDbContext UserContext = new ApplicationDbContext();
        private static readonly LocalDataContext _db = new LocalDataContext();
        
                public static void CreateUserAsp(string email, string roleName, string password)
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(UserContext));

            var userAsp = new ApplicationUser
            {
                Email = email,
                UserName = email,
            };

            var result =   userManager.Create(userAsp, password);
            if (result.Succeeded)
            {
                userManager.AddToRole(userAsp.Id, roleName);
            }
        }

        //this is the creation of the first user of the system, we need to do it, because we are supossed to secure our app
        public static void CheckSuperUser()
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(UserContext));
            var email = WebConfigurationManager.AppSettings["AdminUser"];
            var password = WebConfigurationManager.AppSettings["AdminPassWord"];
            var userAsp = userManager.FindByName(email);
            if (userAsp == null)
            {
                CreateUserAsp(email, "Admin", password);
                return;
            }

            userManager.AddToRole(userAsp.Id, "Admin");
        }

        //to create a given role if we need it
        public static void CheckRole(string roleName)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(UserContext));

            // Check to see if Role Exists, if not create it
            if (!roleManager.RoleExists(roleName))
            {
                roleManager.Create(new IdentityRole(roleName));
            }
        }

        public void Dispose()
        {
          UserContext.Dispose();
            _db.Dispose();
        }
    }
}