using Jmo.Backend.Data;
using Jmo.Backend.Helpers;
using Jmo.Backend.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jmo.Backend
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddIdentity<ApplicationUser, IdentityRole>(cfg =>
            //    {
            //        cfg.User.RequireUniqueEmail = true;
            //        cfg.Password.RequireDigit = false;
            //        cfg.Password.RequiredUniqueChars = 0;
            //        cfg.Password.RequireLowercase = false;
            //        cfg.Password.RequireNonAlphanumeric = false;
            //        cfg.Password.RequireUppercase = false;
            //    })
            //    .AddEntityFrameworkStores<DataContext>();

            //services.AddDbContext<DataContext>(cfg =>
            //{
            //    cfg.UseSqlServer(this.Configuration.GetConnectionString("DefaultConnection"));
            //});

            //   services.AddTransient<SeedDb>();

            // services.AddScoped<IRepository, Repository>();
            services.AddScoped<IUserHelper, UserHelper>();
            services.AddScoped<IPreguntaRepository, PreguntaRepository>();
            services.AddScoped<ICategoriaRepository, CategoriaRepository>();
            services.AddScoped<IRespuestaRepository, RespuestaRepository>();


            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
