using Core.Interfaces;
using Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonalPhotos.Filters;
using System.Reflection;

namespace PersonalPhotos
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
            services.AddMvc();
            services.AddSession();
            services.AddScoped<ILogins, SqlServerLogins>();
            services.AddSingleton<IKeyGenerator, DefaultKeyGenerator>();
            services.AddScoped<IPhotoMetaData, SqlPhotoMetaData>();
            services.AddScoped<IFileStorage, LocalFileStorage>();
            services.AddScoped<LoginAttribute>();

            var connectionString = Configuration.GetConnectionString("Default");
            var currentAssembly = Assembly.GetExecutingAssembly().GetName().Name;


            services.AddDbContext<IdentityDbContext>(opt => 
            {
                opt.UseSqlServer(connectionString, obj => obj.MigrationsAssembly(currentAssembly));
            });

            services.AddIdentity<IdentityUser, IdentityRole>(opt => 
            {
                opt.Password = new PasswordOptions
                {
                    RequireDigit = false,
                    RequiredLength = 3,
                    RequiredUniqueChars = 3
                };

                opt.User = new UserOptions
                {
                    RequireUniqueEmail = true
                };

                opt.SignIn = new SignInOptions
                {
                    RequireConfirmedEmail = false,
                    RequireConfirmedPhoneNumber = false
                };

                opt.Lockout = new LockoutOptions
                {
                    AllowedForNewUsers = false,
                    DefaultLockoutTimeSpan = new System.TimeSpan(0, 15, 0),
                    MaxFailedAccessAttempts = 3
                };
            })
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(opt =>
            {
                opt.LoginPath = "/Logins/Index";
            });
            services.AddAuthorization(opt =>
            {
                opt.AddPolicy("EditorOver18Policy", policy => 
                {
                    policy
                    .RequireClaim("Over18Claim");
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseSession();
            app.UseAuthentication();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    "default",
                    "{controller=Photos}/{action=Display}");
            });
        }
    }
}