using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace PredictLeague.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SignInManager<IdentityUser> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            
            // Пренасочваме към същата страница с GET, за да се изчисти хедъра!
            return RedirectToPage("./Logout", new { loggedOut = true });
        }

        public async Task<IActionResult> OnGet(bool? loggedOut)
        {
            if (loggedOut == true)
            {
                return Page();
            }

            // Ако потребителят е влязъл и просто отвори страницата с GET, го пращаме в началото
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToPage("/Index");
            }

            return Page();
        }
    }
}
