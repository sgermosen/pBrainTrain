using pBrainTrain.Backend.Helpers;
using pBrainTrain.Backend.Models;
using System.Data.Entity;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using static System.Data.Entity.Migrations.Model.UpdateDatabaseOperation;

namespace pBrainTrain.Backend
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Database.SetInitializer(
                new MigrateDatabaseToLatestVersion<LocalDataContext, Migrations.Configuration>());//this is to specify that on each time than the main proyect (on this case, the backend runs) the database be updated (fields and data) with the currrent schema
            //but obviously it need to be inivoked
            CheckRolesAndSuperUser();
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        private void CheckRolesAndSuperUser()
        {
            //this will be run just one time, on the first run of your application
            UsersHelper.CheckRole("Admin");
            UsersHelper.CheckRole("Moderator");
            UsersHelper.CheckRole("User");
            UsersHelper.CheckSuperUser();
        }
    }
}
