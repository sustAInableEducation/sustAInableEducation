using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using sustAInableEducation_backend.Models;

namespace sustAInableEducation_backend.Repository
{
    public class DataSeeder
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public DataSeeder(
            IConfiguration config, 
            ApplicationDbContext context, 
            RoleManager<IdentityRole> roleManager, 
            UserManager<ApplicationUser> userManager
            )
        {
            _config = config;
            _context = context;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task Seed()
        {
            if (!await _context.Setting.AnyAsync())
            {
                _context.Setting.Add(new Setting
                {
                    Id = "AllowRegistration",
                    Value = "true"
                });
                _context.Setting.Add(new Setting
                {
                    Id = "AllowSpaceCreation",
                    Value = "true"
                });
                await _context.SaveChangesAsync();
            }

            if (await _roleManager.FindByNameAsync("Admin") == null)
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            if (!await _context.Users.AnyAsync(u => u.UserName == _config["AdminEmail"]))
            {
                var user = new ApplicationUser
                {
                    UserName = _config["AdminEmail"],
                    Email = _config["AdminEmail"]
                };
                await _userManager.CreateAsync(user, _config["AdminPassword"]!);
                await _userManager.AddToRoleAsync(user, "Admin");
            }
        }
    }
}
