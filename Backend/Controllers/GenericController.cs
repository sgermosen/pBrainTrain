using System.Linq;
using System.Web.Mvc;
using Backend.Models;

namespace Backend.Controllers
{
    [Authorize(Roles = "Admin")]
    public class GenericController : Controller
    {
        private DataContextLocal db = new DataContextLocal();

        public JsonResult GetTeams(int leagueId)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var teams = db.Teams.Where(m => m.LeagueId == leagueId);
            return Json(teams);
        }
        //public JsonResult GetCities(int departmentId)
        //{
        //    db.Configuration.ProxyCreationEnabled = false;
        //    var cities = db.Cities.Where(c => c.DepartmentId == departmentId);
        //    return Json(cities);
        //}

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

       
    }
}