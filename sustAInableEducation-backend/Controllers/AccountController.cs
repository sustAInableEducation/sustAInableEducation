﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using sustAInableEducation_backend.Models;
using sustAInableEducation_backend.Repository;
using System.Security.Claims;

namespace sustAInableEducation_backend.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly string _userId;
        private readonly ApplicationUser _user;

        public AccountController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            _user = _context.Users.Find(_userId)!;
        }

        [HttpGet]
        public ActionResult<ApplicationUser> GetAccount()
        {
            return _user;
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(SignInManager<ApplicationUser> signInManager)
        {
            await signInManager.SignOutAsync().ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("changeEmail")]
        public async Task<IActionResult> ChangeEmail(ChangeEmailRequest request, UserManager<ApplicationUser> userManager)
        {
            var userNameResult = await userManager.SetUserNameAsync(_user, request.NewEmail).ConfigureAwait(false);
            var emailResult = await userManager.SetEmailAsync(_user, request.NewEmail).ConfigureAwait(false);
            if (!userNameResult.Succeeded || !emailResult.Succeeded)
            {
                return BadRequest(userNameResult.Succeeded ? emailResult.Errors : userNameResult.Errors);
            }
            return NoContent();
        }

        [HttpPost("changePassword")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, UserManager<ApplicationUser> userManager)
        {
            var result = await userManager.ChangePasswordAsync(_user, request.OldPassword, request.NewPassword).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            return NoContent();
        }
    }
}
