using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Domain;

namespace Backend.Controllers
{
    public class GroupUsersController : Controller
    {
        private DataContext db = new DataContext();

        // GET: GroupUsers
        public async Task<ActionResult> Index()
        {
            var groupUsers = db.GroupUsers.Include(g => g.Group).Include(g => g.User);
            return View(await groupUsers.ToListAsync());
        }

        // GET: GroupUsers/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GroupUser groupUser = await db.GroupUsers.FindAsync(id);
            if (groupUser == null)
            {
                return HttpNotFound();
            }
            return View(groupUser);
        }

        // GET: GroupUsers/Create
        public ActionResult Create()
        {
            ViewBag.GroupId = new SelectList(db.Groups, "GroupId", "Name");
            ViewBag.UserId = new SelectList(db.Users, "UserId", "FirstName");
            return View();
        }

        // POST: GroupUsers/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "GroupUserId,GroupId,UserId,IsAccepted,IsBlocked,Points")] GroupUser groupUser)
        {
            if (ModelState.IsValid)
            {
                db.GroupUsers.Add(groupUser);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.GroupId = new SelectList(db.Groups, "GroupId", "Name", groupUser.GroupId);
            ViewBag.UserId = new SelectList(db.Users, "UserId", "FirstName", groupUser.UserId);
            return View(groupUser);
        }

        // GET: GroupUsers/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GroupUser groupUser = await db.GroupUsers.FindAsync(id);
            if (groupUser == null)
            {
                return HttpNotFound();
            }
            ViewBag.GroupId = new SelectList(db.Groups, "GroupId", "Name", groupUser.GroupId);
            ViewBag.UserId = new SelectList(db.Users, "UserId", "FirstName", groupUser.UserId);
            return View(groupUser);
        }

        // POST: GroupUsers/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "GroupUserId,GroupId,UserId,IsAccepted,IsBlocked,Points")] GroupUser groupUser)
        {
            if (ModelState.IsValid)
            {
                db.Entry(groupUser).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.GroupId = new SelectList(db.Groups, "GroupId", "Name", groupUser.GroupId);
            ViewBag.UserId = new SelectList(db.Users, "UserId", "FirstName", groupUser.UserId);
            return View(groupUser);
        }

        // GET: GroupUsers/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GroupUser groupUser = await db.GroupUsers.FindAsync(id);
            if (groupUser == null)
            {
                return HttpNotFound();
            }
            return View(groupUser);
        }

        // POST: GroupUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            GroupUser groupUser = await db.GroupUsers.FindAsync(id);
            db.GroupUsers.Remove(groupUser);
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
