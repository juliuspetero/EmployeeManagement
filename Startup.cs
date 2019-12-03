using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmployeeManagement.Models;
using EmployeeManagement.Security;
using EmployeeManagement.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement
{
    public class Startup
    {
        private IConfiguration _config;

        public Startup(IConfiguration config)
        {
            _config = config;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContextPool<AppDbContext>(options => 
            options.UseSqlServer(_config.GetConnectionString("EmployeeDBConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 0;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                // This allow the user to first confirm their email before they could sign in
                options.SignIn.RequireConfirmedEmail = true;

                // Set the email confirmation token provider to our custom provider
                options.Tokens.EmailConfirmationTokenProvider = "CustomEmailConfirmation";
            }).AddEntityFrameworkStores<AppDbContext>()

            // Generate token by default for 2 factor auth, reset password etc.
              .AddDefaultTokenProviders()

            // Add a custom token provider other than the default one
            .AddTokenProvider<CustomEmailConfirmationTokenProvider<ApplicationUser>>("CustomEmailConfirmation");

            // Changes token lifespan of all token types to 5 hours
            services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(5));

            // Change the lifespan of email confirmation token to 3 days, this just override for email confirmation or Custom class inherits from the DataProtection above
            services.Configure<CustomEmailConfirmationTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromDays(3));

            // Change access denied route from "Account/AccessDenied"
            services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = new PathString("/Administration/AccessDenied");
            });

            // Apply Authorize attribute globally and redirect a non logged in user to the login page
            services.AddMvc(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                                    .RequireAuthenticatedUser()
                                    .Build(); 
                options.Filters.Add(new AuthorizeFilter(policy)); 
            }).AddXmlSerializerFormatters();

            // Configure google as external login provider
            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = "235705716122-1kgffgtv3ic2phao2vq6jk8219vv5la6.apps.googleusercontent.com";
                    options.ClientSecret = "tY9W1Z8Bx5juU519UFbM8lnv";
                });

            // Add all your policies here
            services.AddAuthorization(options =>
            {
                // For this policy to be fullfilled, a user must have Delete Role wuth a value of true 
                options.AddPolicy("DeleteRolePolicy", 
                    policy => policy.RequireClaim("Delete Role", "true"));

                // The a logged in user must be in the admin role and must have edit role claim or he must 
                //be a member of the super admin role using RequireAssertion
                //options.AddPolicy("EditRolePolicy",
                //    policy => policy.RequireAssertion(context =>
                //    context.User.IsInRole("Admin") &&
                //    context.User.HasClaim(claim => claim.Type == "Edit Role" && claim.Value == "true") ||
                //    context.User.IsInRole("Super Admin")
                //    ));

                //Add Policy using custom requirement
                options.AddPolicy("EditRolePolicy",
                    policy => policy.AddRequirements(new ManageAdminRolesAndClaimsRequirement()));

                // Other handlers will not be invoked when one of the handler return failure
                //options.InvokeHandlersAfterFailure = false;

                // This makes role also policy-based
                options.AddPolicy("AdminRolePolicy",
                    policy => policy.RequireRole("Admin"));
            });

            // Sending email configuration  settings
            services.Configure<EmailSettings>(_config.GetSection("EmailSettings"));

            // When the service of sending email is needed only IEmail is injected in the constructor
            services.AddSingleton<IEmailSender, EmailSender>();

            services.AddScoped<IEmployeeRepository, SQLEmployeeRepository>();

            // Registering IAuthorizationHandler services with CanEditOnlyOtherAdminRolesAndClaimsHandler 
            services.AddSingleton<IAuthorizationHandler, CanEditOnlyOtherAdminRolesAndClaimsHandler>();

            // Do not forget to register all the handlers for your requirement
            services.AddSingleton<IAuthorizationHandler, SuperAdminHandler>();

            services.AddSingleton<DataProtectionPurposeStrings>();
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
                app.UseExceptionHandler("/Error");
                app.UseStatusCodePagesWithReExecute("/Error/{0}");
            }

            app.UseStaticFiles();
            //app.UseMvcWithDefaultRoute();

            app.UseAuthentication();

            app.UseMvc(routes => 
            {
                routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
