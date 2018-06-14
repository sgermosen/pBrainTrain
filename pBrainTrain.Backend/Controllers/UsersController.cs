namespace pBrainTrain.Backend.Controllers
{
    using System.Data.Entity;
    using System.Threading.Tasks;
    using System.Net;
    using System.Web.Mvc;
    using pBrainTrain.Backend.Models;
    using pBrainTrain.Domain;
    using pBrainTrain.Backend.Helpers;
    using System.Collections.Generic;
    using System.Linq;
    using System.Data.Entity.Validation;

    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private LocalDataContext db = new LocalDataContext();


        // GET: Users/Edit/5
        public async Task<ActionResult> AssignRol(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = await db.Users.FindAsync(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            //but we never pass it, so he is intelligent, but not mage

            ViewBag.RolId = new SelectList(db.Rols, "RolId", "Name");

            var uRol = new UserRol();
            uRol.UserId = user.UserId;
            return View(uRol);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AssignRol(UserRol uRol)
        {
            if (ModelState.IsValid)
            {
                //sorry, is the 3 days without sleep :'(
                uRol.StatusId = 1;
                db.UserRols.Add(uRol);
               
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbEntityValidationException e)
                {
                    var message = string.Empty;
                    foreach (var eve in e.EntityValidationErrors)
                    {

                        //Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        //    eve.Entry.Entity.GetType().Name, eve.Entry.State);
                        message = string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                            eve.Entry.Entity.GetType().Name, eve.Entry.State);

                        foreach (var ve in eve.ValidationErrors)
                        {
                            //Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            //    ve.PropertyName, ve.ErrorMessage);
                            message += string.Format("\n- Property: \"{0}\", Error: \"{1}\"",
                                ve.PropertyName, ve.ErrorMessage);
                        }
                    }

                }

                catch (System.Exception e)
                {

                    throw;
                }
                //Do you remember? never use it again please return RedirectToAction("Details/" + uRol.UserId);
                return RedirectToAction(string.Format("Details/{0}", uRol.UserId));
            }
            ViewBag.RolId = new SelectList(db.Rols, "RolId", "Name", uRol.RolId);

            return View(uRol);
        }

        // GET: Users
        public async Task<ActionResult> Index()
        {
            var users = db.Users.Include(u => u.UserType).Include(p => p.UserRols);
            return View(await users.ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var user = await db.Users.FindAsync(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            user.UserRols = new List<UserRol>();

            var userRols = await db.UserRols.Where(r => r.UserId == user.UserId).ToListAsync();

            if (userRols != null)
            {
                foreach (var item in userRols)
                {
                    user.UserRols.Add(new UserRol
                    {
                        UserId = item.UserId,
                        RolId = item.RolId,
                        UserRolId = item.UserRolId
                    });
                }

            }

            return View(user);
        }


        public ActionResult Create()
        {
            //normally the viewbag is named as the primary key from the table than we want to be listed
            ViewBag.UserTypeId = new SelectList(db.UserTypes, "UserTypeId", "Name"); //the id value (name of primary key) and the show value, I mean, the data that the user will see
            ViewBag.StatusId = new SelectList(db.Status, "StatusId", "Name");
            ViewBag.CountryId = new SelectList(db.Countries, "CountryId", "Name");

            var userView = new UserView();

            return View(userView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(UserView view)
        {
            if (view.Password == view.PasswordConfirm)
            {
                if (ModelState.IsValid)
                {
                    var pic = string.Empty;
                    const string folder = "~/Content/Users";

                    if (view.ImageFile!=null)
                    {
                        pic = Files.UploadPhoto(view.ImageFile, folder, "");
                        pic = string.Format("{0}/{1}", folder, pic);
                    }

                    var user = new User
                    {
                        FirstName = view.FirstName,
                        LastName = view.LastName,
                        Email = view.Email,
                        UserTypeId = view.UserTypeId,
                        StatusId = view.StatusId,
                        CountryId = view.CountryId
                    };

                    user.Picture = pic;

                    db.Users.Add(user);
                    await db.SaveChangesAsync(); //username on this case email because must me unique for me, then the asigned rol, and finnaly the password
                    UsersHelper.CreateUserAsp(view.Email, "User", view.Password);
                    return RedirectToAction("Index");
                }
            }
            ViewBag.UserTypeId = new SelectList(db.UserTypes, "UserTypeId", "Name", view.UserTypeId);
            ViewBag.StatusId = new SelectList(db.Status, "StatusId", "Name", view.StatusId);
            ViewBag.CountryId = new SelectList(db.Countries, "CountryId", "Name", view.CountryId);

            return View(view);
        }

        // GET: Users/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = await db.Users.FindAsync(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            ViewBag.UserTypeId = new SelectList(db.UserTypes, "UserTypeId", "Name", user.UserTypeId);
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "UserId,FirstName,LastFirstName,UserTypeId,Picture,Email")] User user)
        {
            if (ModelState.IsValid)
            {
                db.Entry(user).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.UserTypeId = new SelectList(db.UserTypes, "UserTypeId", "Name", user.UserTypeId);
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = await db.Users.FindAsync(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            User user = await db.Users.FindAsync(id);
            db.Users.Remove(user);
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
