using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web.Mvc;
using Backend.Models;
using Domain;
using System;
using PsTools;

namespace Backend.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LeaguesController : Controller
    {
        private DataContextLocal db = new DataContextLocal();


        #region TeamsController


        //public async Task<ActionResult> DetailsTeam(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }

        //    var team = await db.Teams.FindAsync(id);

        //    if (team == null)
        //    {
        //        return HttpNotFound();
        //    }

        //    return View(team);
        //}

        //public async Task<ActionResult> DetailsTeam(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }

        //    var team = await db.Teams.FindAsync(id);

        //    if (team == null)
        //    {
        //        return HttpNotFound();
        //    }

        //    var view = ToViewTeam(team);
        //    return View(view);
        //}

        //public async Task<ActionResult> DetailsTeam(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }

        //    var team = await db.Teams.FindAsync(id);

        //    if (team == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    var view = ToViewTeam(team);
        //    return View(view);
        //}

        private Team ToTeam(TeamView view)
        {
            return new Team
            {
                LeagueId = view.LeagueId,
                Logo = view.Logo,
                Name = view.Name,
                Initials = view.Initials,
                League = view.League,
                TeamId = view.TeamId
            };
        }
        private TeamView ToViewTeam(Team team)
        {
            return new TeamView
            {
                LeagueId = team.LeagueId,
                Logo = team.Logo,
                Name = team.Name,
                Initials = team.Initials,
                League = team.League,
                TeamId = team.TeamId
            };
        }
        public async Task<ActionResult> CreateTeam(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var league = await db.Leagues.FindAsync(id);

            if (league == null)
            {
                return HttpNotFound();
            }

            var view = new TeamView { LeagueId = league.LeagueId, };
            return View(view);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateTeam(TeamView view)
        {
            if (ModelState.IsValid)
            {
                var table = db.Teams
                    .Where(u => u.Name.ToLower() == view.Name.ToLower() && u.LeagueId == view.LeagueId)
                    .FirstOrDefaultAsync();

                if (table.Result != null)
                {
                    ModelState.AddModelError(string.Empty,
                        "Este nombre ya esta en uso en esta liga, escoja uno diferente");

                }
                else
                {
                    var table2 = db.Teams
                        .Where(u => u.Initials.ToLower() == view.Initials.ToLower() && u.LeagueId == view.LeagueId)
                        .FirstOrDefaultAsync();

                    if (table2.Result != null)
                    {
                        ModelState.AddModelError(string.Empty,
                            "Estas iniciales ya estan en uso en esta liga, escoja uno diferente");
                    }
                    else
                    {

                        var pic = string.Empty;
                        var folder = "~/Content/Logos";

                        if (view.LogoFile != null)
                        {
                            pic = FilesHelper.UploadPhoto(view.LogoFile, folder);
                            pic = string.Format("{0}/{1}", folder, pic);
                        }

                        var team = ToTeam(view);
                        team.Logo = pic;

                        db.Teams.Add(team);
                        await db.SaveChangesAsync();

                        return RedirectToAction(string.Format("Details/{0}", team.LeagueId));
                    }
                }
            }

            return View(view);
        }

        public async Task<ActionResult> EditTeam(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var team = await db.Teams.FindAsync(id);

            if (team == null)
            {
                return HttpNotFound();
            }

            var view = ToViewTeam(team);
            return View(view);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditTeam(TeamView view)
        {
            if (ModelState.IsValid)
            {
                var table = db.Teams
                    .Where(u => u.Name.ToLower() == view.Name.ToLower() && u.LeagueId == view.LeagueId && u.TeamId != view.TeamId)
                    .FirstOrDefaultAsync();

                if (table.Result != null)
                {
                    ModelState.AddModelError(string.Empty,
                        "Este nombre ya esta en uso en esta liga, escoja uno diferente");
                }
                else
                {
                    var table2 = db.Teams
                        .Where(u => u.Initials.ToLower() == view.Initials.ToLower() && u.LeagueId == view.LeagueId && u.TeamId != view.TeamId)
                        .FirstOrDefaultAsync();

                    if (table2.Result != null)
                    {
                        ModelState.AddModelError(string.Empty,
                            "Estas iniciales ya estan en uso en esta liga, escoja uno diferente");
                    }
                    else
                    {
                        if (ModelState.IsValid)
                        {
                            var pic = view.Logo;
                            var folder = "~/Content/Logos";

                            if (view.LogoFile != null)
                            {
                                pic = FilesHelper.UploadPhoto(view.LogoFile, folder);
                                pic = string.Format("{0}/{1}", folder, pic);
                            }

                            var team = ToTeam(view);
                            team.Logo = pic;

                            db.Entry(team).State = EntityState.Modified;
                            await db.SaveChangesAsync();
                            return RedirectToAction(string.Format("Details/{0}", team.LeagueId));
                        }
                    }
                }
            }

            return View(view);
        }

        public async Task<ActionResult> DeleteTeam(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var team = await db.Teams.FindAsync(id);

            if (team == null)
            {
                return HttpNotFound();
            }

            db.Teams.Remove(team);
            await db.SaveChangesAsync();
            return RedirectToAction(string.Format("Details/{0}", team.LeagueId));
        }


        #endregion

        #region LeagueController

        public async Task<ActionResult> Index()
        {
            return View(await db.Leagues.ToListAsync());
        }

        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var league = await db.Leagues.FindAsync(id);

            if (league == null)
            {
                return HttpNotFound();
            }

            return View(league);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(LeagueView view)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    //  var nombre = await db.Leagues.FindAsync(view.Name);

                    var table = db.Leagues
                        .Where(u => u.Name.ToLower() == view.Name.ToLower())
                        .FirstOrDefaultAsync();

                    if (table.Result != null)
                    {
                        ModelState.AddModelError(string.Empty, "Este nombre ya esta en uso, escoja uno diferente");

                    }
                    else
                    {
                        var pic = string.Empty;
                        var folder = "~/Content/Logos";

                        if (view.LogoFile != null)
                        {
                            pic = FilesHelper.UploadPhoto(view.LogoFile, folder);
                            pic = string.Format("{0}/{1}", folder, pic);
                        }

                        var league = ToLeague(view);
                        league.Logo = pic;

                        db.Leagues.Add(league);
                        await db.SaveChangesAsync();
                        return RedirectToAction("Index");
                    }
                }

                return View(view);
            }
            catch (Exception e)
            {
                string message = string.Format("Message: {0}", e.Message);
                message += string.Format("Inner: {0}", e.InnerException);
                ModelState.AddModelError(string.Empty, message);
                return View(view);
            }
        }


        //protected override void OnException(ExceptionContext filterContext)
        //{
        //    Exception exception = filterContext.Exception;
        //    //Logging the Exception
        //    filterContext.ExceptionHandled = true;


        //    var Result = this.View("Error", new HandleErrorInfo(exception,
        //        filterContext.RouteData.Values["controller"].ToString(),
        //        filterContext.RouteData.Values["action"].ToString()));

        //    filterContext.Result = Result;

        //}

        private League ToLeague(LeagueView view)
        {
            return new League
            {

                LeagueId = view.LeagueId,
                Logo = view.Logo,
                Name = view.Name,
                Teams = view.Teams
            };
        }

        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var league = await db.Leagues.FindAsync(id);

            if (league == null)
            {
                return HttpNotFound();
            }
            // ViewBag.TeamId = new SelectList(db.Leagues, "TeamId", "Name", Leagues.TeamId);

            var view = ToView(league);
            return View(view);
        }

        private LeagueView ToView(League league)
        {
            return new LeagueView
            {

                LeagueId = league.LeagueId,
                Logo = league.Logo,
                Name = league.Name,
                Teams = league.Teams
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(LeagueView view)
        {
            try
            {
                var table = db.Leagues
                .Where(u => u.Name.ToLower() == view.Name.ToLower() && u.LeagueId != view.LeagueId)
                .FirstOrDefaultAsync();

                if (table.Result != null)
                {
                    ModelState.AddModelError(string.Empty, "Este nombre ya esta en uso, escoja uno diferente");

                }
                else
                {


                    if (ModelState.IsValid)
                    {
                        var pic = view.Logo;
                        var folder = "~/Content/Logos";

                        if (view.LogoFile != null)
                        {
                            pic = FilesHelper.UploadPhoto(view.LogoFile, folder);
                            pic = string.Format("{0}/{1}", folder, pic);
                        }

                        var league = ToLeague(view);
                        league.Logo = pic;

                        db.Entry(league).State = EntityState.Modified;
                        await db.SaveChangesAsync();
                        return RedirectToAction("Index");
                    }
                }
                return View(view);
            }
            catch (Exception e)
            {
                string message = string.Format("Message: {0}", e.Message);
                message += string.Format("Inner: {0}", e.InnerException);
                ModelState.AddModelError(string.Empty, message);
                return View(view);
            }
        }

        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            League league = await db.Leagues.FindAsync(id);
            if (league == null)
            {
                return HttpNotFound();
            }
            return View(league);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            League league = await db.Leagues.FindAsync(id);
            db.Leagues.Remove(league);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
