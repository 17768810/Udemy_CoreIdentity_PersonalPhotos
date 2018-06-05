using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PersonalPhotos.Models;

namespace PersonalPhotos.Controllers
{
    public class LoginsController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogins _loginService;

        public LoginsController(ILogins loginService, 
            IHttpContextAccessor httpContextAccessor, 
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _loginService = loginService;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        public IActionResult Index(string returnUrl = null)
        {
            var model = new LoginViewModel { ReturnUrl = returnUrl};
            return View("Login", model);
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Invalid login detils");
                return View("Login", model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

            if(!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Username and/or password is incorrect");
                return View();
            }

            //claims
            var claims = new List<Claim>();
            claims.Add(new Claim("Over18Claim", "True"));
            //claims.Add(new Claim("Over18Claim", "True"));
            var claimIdentity = new ClaimsIdentity(claims);
            User.AddIdentity(claimIdentity);


            if (!string.IsNullOrEmpty(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            else
            {
                return RedirectToAction("Display", "Photos");
            }
        }

        public IActionResult Create()
        {
            return View("Create");
        }

        [HttpPost]
        public async Task<IActionResult> Create(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Invalid user details");
                return View(model);
            }

            // Add Role if not exists
            if (!(await _roleManager.RoleExistsAsync("Editor")))
            {
                await _roleManager.CreateAsync(new IdentityRole("Editor"));
            }

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if(!result.Succeeded)
            {
                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, $"{error.Code}-{error.Description}");
                }
            }

            // Add User to Role
            //if(User.IsInRole("Editor"))
            //{

            //}
            await _userManager.AddToRoleAsync(user, "Editor");

            return RedirectToAction("Index", "Logins");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Logins");
        }
    }
}