using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using pBrainTrain.API.Helpers;
using pBrainTrain.API.Models;
using pBrainTrain.Backend.Helpers;
using pBrainTrain.Domain;

namespace pBrainTrain.API.Controllers
{
    [RoutePrefix("api/Users")]
    public class UsersController : ApiController
    {
        private DataContext db = new DataContext();

        // GET: api/Users
        public IQueryable<User> GetUsers()
        {//if we are sure than we put jsonignore to all virtual properties, we disabled this, 
            db.Configuration.ProxyCreationEnabled = false;
            return db.Users;
        }

        // GET: api/Users/5
        [ResponseType(typeof(User))]
        public async Task<IHttpActionResult> GetUser(int id)
        {
            var user = await db.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var userResponse = new UserResponse
            {
                Email = user.Email,
                CountryId = user.CountryId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Picture = user.Picture,
                StatusId = user.StatusId,
                UserId = user.UserId,
                UserTypeId = user.UserTypeId,
                Country = user.Country,
                Status = user.Status,
                //UserRols = user.UserRols.ToList(), //we dont need to retrieve this information, but if you need it, this is the way
                UserType = user.UserType

            };


            return Ok(userResponse);
        }

        // PUT: api/Users/5 update
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

            var oldUserData = await db.Users.FindAsync(id);
            if (oldUserData == null)
            {
                return NotFound();
            }
            var oldEmail = oldUserData.Email;
            var isEmailChanged = oldUserData.Email.ToLower() != request.Email.ToLower();

            if (request.PictureArray != null && request.PictureArray.Length>0)
            {
                var stream = new MemoryStream(request.PictureArray);
                var guid = Guid.NewGuid().ToString(); //aleatoryname for the image
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
                request.Picture = oldUserData.Picture;
            }

            oldUserData.Email = request.Email;
            oldUserData.FirstName = request.FirstName;
            oldUserData.LastName = request.LastName;
            oldUserData.Picture = request.Picture;
            oldUserData.CountryId = request.CountryId;
 
            db.Entry(oldUserData).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
                if (isEmailChanged)
                {
                    Helpers.UsersHelper.UpdateEmail(oldEmail, request.Email);
                }
                return Ok(oldUserData);
            }
            catch ( Exception ex)
            {
                return BadRequest(ex.Message);
            }            
        }

        // POST: api/Users insert
        [ResponseType(typeof(User))]
        public async Task<IHttpActionResult> PostUser(UserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.PictureArray != null && request.PictureArray.Length > 0)
            {
                var stream = new MemoryStream(request.PictureArray);
                var guid = Guid.NewGuid().ToString(); //aleatoryname for the image
                var file = string.Format("{0}.jpg", guid);
                var folder = "~/Content/Users";
                var fullPath = string.Format("{0}/{1}", folder, file);
                var response = FilesHelper.UploadPhoto(stream, folder, file);

                if (response)
                {
                    request.Picture = fullPath;
                }
            }
            var user = new User
            {
                Picture = request.Picture,
                CountryId = request.CountryId,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                StatusId = request.StatusId,
                UserTypeId = request.UserTypeId
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();
            Helpers.UsersHelper.CreateUserAsp(request.Email,"User",request.Password);

            return CreatedAtRoute("DefaultApi", new { id = user.UserId }, user);
        }
        //private void CreateUserAsp(string email, string roleName, string password)
        //{
        //    var userContext = new ApplicationDbContext();
        //    var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(userContext));

        //    var userAsp = new ApplicationUser
        //    {
        //        Email = email,
        //        UserName = email,
        //    };

        //    var result = userManager.Create(userAsp, password);
        //    if (result.Succeeded)
        //    {
        //        userManager.AddToRole(userAsp.Id, roleName);
        //    }
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