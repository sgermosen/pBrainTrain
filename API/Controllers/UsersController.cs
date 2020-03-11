using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using API.Classes;
using API.Models;
using Domain;
using Newtonsoft.Json.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace API.Controllers
{
    //[Authorize(Roles = "User")]
    [RoutePrefix("api/Users")]

    public class UsersController : ApiController
    {
        private DataContext db = new DataContext();

        [HttpGet]
        [Route("GetPoints/{id}")]
        public async Task<IHttpActionResult> GetPoints(int id)
        {
            var user = await db.Users.FindAsync(id);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            return Ok(new PointsResponse {Points=user.Points} );
        }

        [HttpPost]
        [Route("ChangePassword")]
        public async Task<IHttpActionResult> ChangePassword(JObject form)
        {
            string email;
            string currentPassword;
            string newPassword;

            dynamic jsonObject = form;

            try
            {
                email = jsonObject.Email.Value;
                currentPassword = jsonObject.CurrentPassword.Value;
                newPassword = jsonObject.NewPassword.Value;
            }
            catch
            {
                return BadRequest("Incorrect call");
            }
            var userContext = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(userContext));

            var userAsp = userManager.FindByEmail(email);
            if (userAsp == null)
            {
                return BadRequest("Incorrect call");
            }
            var response = await userManager.ChangePasswordAsync(userAsp.Id, currentPassword, newPassword);

            if (response.Succeeded)
            {
                return BadRequest(response.Errors.FirstOrDefault());
            }
            return Ok("ok");

        }

        [HttpPost]
        [Route("GetUserByEmail")]
        public async Task<IHttpActionResult> GetUserByEmail(JObject form)
        {
            string email;
            dynamic jsonObject = form;

            try
            {
                email = jsonObject.Email.Value;
            }
            catch
            {
                return BadRequest("Incorrect call");
            }

            // var user = await db.Users.Where(u => u.Email.ToLower() == email.ToLower()).FirstOrDefaultAsync();
            var user = await db.Users
                .Where(u => u.Email.ToLower() == email.ToLower())
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            var userResponse = ToUserResponse(user);
            return Ok(userResponse);
        }

        public IQueryable<User> GetUsers()
        {
            //si estoy trabajando con una sola tabla donde esta todo, puedo solucionarlo asi
            //sin embargo no la necesito si pongo el jsonignore en cada virtual
            db.Configuration.ProxyCreationEnabled = false;
            return db.Users;
        }


        [ResponseType(typeof(User))]
        public async Task<IHttpActionResult> GetUser(int id)
        {
            //si estoy trabajando con una sola tabla donde esta todo, puedo solucionarlo asi
            // db.Configuration.ProxyCreationEnabled = false;
            User user = await db.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var userResponse = ToUserResponse(user);
            return Ok(userResponse);
        }

        private UserResponse ToUserResponse(User user)
        {
            return new UserResponse
            {
                Email = user.Email,
                FavoriteTeam = user.FavoriteTeam,
                FavoriteTeamId = user.FavoriteTeamId,
                FirstName = user.FirstName,
                // GroupUsers = user.GroupUsers.ToList(),
                LastName = user.LastName,
                NickName = user.NickName,
                Picture = user.Picture,
                Points = user.Points,
                // Predictions = user.Predictions.ToList(),
                // UserGroups = user.UserGroups.ToList(),
                UserId = user.UserId,
                //   UserType = user.UserType,
                UserTypeId = user.UserTypeId
            };
        }

        // PUT: api/Users/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutUser(int id, UserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != request.UserId)
            {
                return BadRequest();
            }

            var oldUser = await db.Users.FindAsync(id);
            if (oldUser == null)
            {
                return NotFound();
            }
            var oldEmail = oldUser.Email;
            var isEmailChanged = oldUser.Email.ToLower() != request.Email.ToLower();

            if (request.ImageArray != null && request.ImageArray.Length > 0)
            {
                var stream = new MemoryStream(request.ImageArray);
                var guid = Guid.NewGuid().ToString();
                var file = string.Format("{0}.jpg", guid);
                var folder = "~/Content/Users";
                var fullPath = string.Format("{0}/{1}", folder, file);
                var response = FilesHelper.UploadPhoto(stream, folder, file);

                if (response)
                {
                    request.Picture = fullPath;
                }
            }
            else
            {
                request.Picture = oldUser.Picture;
            }

            //  var user = ToUser(request);
            oldUser.Email = request.Email;
            oldUser.FavoriteTeamId = request.FavoriteTeamId;
            oldUser.FirstName = request.FirstName;
            oldUser.LastName = request.LastName;
            oldUser.NickName = request.NickName;
            oldUser.Picture = request.Picture;

            db.Entry(oldUser).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
                if (isEmailChanged)
                {
                    UpdateUserName(oldEmail, request.Email);
                }
                return Ok(oldUser);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
                //if (!UserExists(id))
                //{
                //    return NotFound();
                //}
                //else
                //{
                //    throw;
                //}
            }

            //   return StatusCode(HttpStatusCode.NoContent);
        }

        private bool UpdateUserName(string currentUserName, string newUserName)
        {
            var userContext = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(userContext));
            var userASP = userManager.FindByEmail(currentUserName);
            if (userASP == null)
            {
                return false;
            }

            userASP.UserName = newUserName;
            userASP.Email = newUserName;
            var response = userManager.Update(userASP);
            return response.Succeeded;
        }

        [ResponseType(typeof(User))]
        public async Task<IHttpActionResult> PostUser(UserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.ImageArray != null && request.ImageArray.Length > 0)
            {
                var stream = new MemoryStream(request.ImageArray);
                var guid = Guid.NewGuid().ToString();
                var file = string.Format("{0}.jpg", guid);
                var folder = "~/Content/Users";
                var fullPath = string.Format("{0}/{1}", folder, file);
                var response = FilesHelper.UploadPhoto(stream, folder, file);

                if (response)
                {
                    request.Picture = fullPath;
                }
            }

            var user = ToUser(request);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            CreateUserAsp(request.Email, "User", request.Password);

            return CreatedAtRoute("DefaultApi", new { id = user.UserId }, user);
        }

        private void CreateUserAsp(string email, string roleName, string password)
        {
            var userContext = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new Microsoft.AspNet.Identity.EntityFramework.UserStore<ApplicationUser>(userContext));

            var userAsp = new ApplicationUser
            {
                Email = email,
                UserName = email,
            };

            var result = userManager.Create(userAsp, password);
            if (result.Succeeded)
            {
                userManager.AddToRole(userAsp.Id, roleName);
            }
        }

        private User ToUser(UserRequest request)
        {
            return new User
            {
                Email = request.Email,
                FavoriteTeamId = request.FavoriteTeamId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                NickName = request.NickName,
                Picture = request.Picture,
                Points = 0,
                UserTypeId = request.UserTypeId,
            };
        }

        // POST: api/Users
        //[ResponseType(typeof(User))]
        //public async Task<IHttpActionResult> PostUser(User user)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    db.Users.Add(user);
        //    await db.SaveChangesAsync();

        //    return CreatedAtRoute("DefaultApi", new { id = user.UserId }, user);
        //}

        // DELETE: api/Users/5

        [ResponseType(typeof(User))]
        public async Task<IHttpActionResult> DeleteUser(int id)
        {
            User user = await db.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            db.Users.Remove(user);
            await db.SaveChangesAsync();

            return Ok(user);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool UserExists(int id)
        {
            return db.Users.Count(e => e.UserId == id) > 0;
        }

    }
}