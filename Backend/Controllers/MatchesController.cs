using System.Data.Entity;
using System.Threading.Tasks;
using System.Net;
using System.Web.Mvc;
using Backend.Models;
using Domain;

namespace Backend.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MatchesController : Controller
    {
        private DataContextLocal db = new DataContextLocal();

        // GET: Matches
        public async Task<ActionResult> Index()
        {
            var matches = db.Matches.Include(m => m.Date).Include(m => m.Local).Include(m => m.Status).Include(m => m.TournamentGroup).Include(m => m.Visitor);
            return View(await matches.ToListAsync());
        }

        // GET: Matches/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Match match = await db.Matches.FindAsync(id);
            if (match == null)
            {
                return HttpNotFound();
            }
            return View(match);
        }

        // GET: Matches/Create
            // GET: Matches/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Match match = await db.Matches.FindAsync(id);
            if (match == null)
            {
                return HttpNotFound();
            }
            ViewBag.DateId = new SelectList(db.Dates, "DateId", "Name", match.DateId);
            ViewBag.LocalId = new SelectList(db.Teams, "TeamId", "Name", match.LocalId);
            ViewBag.StatusId = new SelectList(db.Status, "StatusId", "Name", match.StatusId);
            ViewBag.TournamentGroupId = new SelectList(db.TournamentGroups, "TournamentGroupId", "Name", match.TournamentGroupId);
            ViewBag.VisitorId = new SelectList(db.Teams, "TeamId", "Name", match.VisitorId);
            return View(match);
        }

        // POST: Matches/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "MatchId,DateId,DateTime,LocalId,VisitorId,LocalGoals,VisitorGoals,StatusId,TournamentGroupId")] Match match)
        {
            if (ModelState.IsValid)
            {
                db.Entry(match).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.DateId = new SelectList(db.Dates, "DateId", "Name", match.DateId);
            ViewBag.LocalId = new SelectList(db.Teams, "TeamId", "Name", match.LocalId);
            ViewBag.StatusId = new SelectList(db.Status, "StatusId", "Name", match.StatusId);
            ViewBag.TournamentGroupId = new SelectList(db.TournamentGroups, "TournamentGroupId", "Name", match.TournamentGroupId);
            ViewBag.VisitorId = new SelectList(db.Teams, "TeamId", "Name", match.VisitorId);
            return View(match);
        }

        // GET: Matches/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Match match = await db.Matches.FindAsync(id);
            if (match == null)
            {
                return HttpNotFound();
            }
            return View(match);
        }

        // POST: Matches/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Match match = await db.Matches.FindAsync(id);
            db.Matches.Remove(match);
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
    }
}
