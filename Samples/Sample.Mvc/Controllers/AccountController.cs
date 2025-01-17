﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents.Session;
using Sample.Mvc.Models;

namespace Sample.Mvc.Controllers
{
    public class AccountController : RavenController
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        
        public AccountController(
            IAsyncDocumentSession dbSession, // injected thanks to Startup.cs call to services.AddRavenDbAsyncSession()
            UserManager<ApplicationUser> userManager, // injected thanks to Startup.cs call to services.AddRavenDbIdentity<AppUser>()
            SignInManager<ApplicationUser> signInManager) // injected thanks to Startup.cs call to services.AddRavenDbIdentity<AppUser>()
            : base(dbSession)
        {
            this._userManager = userManager;
            this._signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult SignIn()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignIn(SignInModel model)
        {
            var signInResult = await _signInManager.PasswordSignInAsync(model.Email, model.Password, true, false);
            if (signInResult.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            var reason = signInResult.IsLockedOut ? "Your user is locked out" :
                signInResult.IsNotAllowed ? "Your user is not allowed to sign in" :
                signInResult.RequiresTwoFactor ? "2FA is required" :
                "Bad user name or password";
            return RedirectToAction("SignInFailure", new { reason });
        }

        [HttpGet]
        public IActionResult SignInFailure(string reason)
        {
            ViewBag.FailureReason = reason;
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            // Create the user.
            var appUser = new ApplicationUser
            {
                Email = model.Email,
                UserName = model.Email
            };
            var createUserResult = await _userManager.CreateAsync(appUser, model.Password);
            if (!createUserResult.Succeeded)
            {
                var errorString = string.Join(", ", createUserResult.Errors.Select(e => e.Description));
                return RedirectToAction("RegisterFailure", new { reason = errorString });
            }

            // Add him to a role.
            await _userManager.AddToRoleAsync(appUser, ApplicationUser.ManagerRole);

            // Sign him in and go home.
            await _signInManager.SignInAsync(appUser, true);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult RegisterFailure(string reason)
        {
            ViewBag.FailureReason = reason;
            return View();
        }

        [HttpGet]
        public IActionResult ChangeRoles()
        {
            return View();
        }

        [Authorize] // Must be logged in to reach this page.
        [HttpPost]
        public async Task<IActionResult> ChangeRoles(ChangeRolesModel model)
        {
            var currentUser = await _userManager.FindByEmailAsync(User.Identity?.Name);
            var currentRoles = await _userManager.GetRolesAsync(currentUser);

            // Add any new roles.
            var newRoles = model.Roles.Except(currentRoles).ToList();
            await _userManager.AddToRolesAsync(currentUser, newRoles);

            // Remove any old roles we're no longer in.
            var removedRoles = currentRoles.Except(model.Roles).ToList();
            await _userManager.RemoveFromRolesAsync(currentUser, removedRoles);
            
            // After we change roles, we need to call SignInAsync before AspNetCore Identity picks up the new roles.
            await _signInManager.SignInAsync(currentUser, true);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public new async Task<IActionResult> SignOut()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangeEmail(string oldEmail, string newEmail)
        {
            var user = await _userManager.FindByEmailAsync(oldEmail);
            user.Email = newEmail;
            var updateResult = await _userManager.UpdateAsync(user);
            return Json(updateResult);
        }
    }
}