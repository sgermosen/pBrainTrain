using Jmo.Backend.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(Jmo.Backend.Areas.Identity.IdentityHostingStartup))]
namespace Jmo.Backend.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {
                services.AddIdentity<ApplicationUser, IdentityRole>(cfg =>
                    {
                        cfg.User.RequireUniqueEmail = true;
                        cfg.Password.RequireDigit = false;
                        cfg.Password.RequiredUniqueChars = 0;
                        cfg.Password.RequireLowercase = false;
                        cfg.Password.RequireNonAlphanumeric = false;
                        cfg.Password.RequireUppercase = false;
                    })
                    .AddEntityFrameworkStores<DataContext>();

                services.AddDbContext<DataContext>(options =>
                    options.UseSqlServer(
                        context.Configuration.GetConnectionString("DefaultConnection")));

                //services.AddDefaultIdentity<ApplicationUser>()
                //    .AddEntityFrameworkStores<DataContext>();
            });
        }
    }
}