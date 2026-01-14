using System;
using System.Linq;
using System.Threading.Tasks;
using AcademicManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademicManagementSystem.Data
{
    public class IdentitySeed
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var db = services.GetRequiredService<ApplicationDbContext>();

            //definiram ulogi 
            string[] roles = { "Admin", "Teacher", "Student" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            //admin
            const string adminEmail = "admin@ams.local";
            const string adminPassword = "Admin123!";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (!result.Succeeded)
                {
                    var msg = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create admin user: {msg}");
                }
            }

            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                await userManager.AddToRoleAsync(adminUser, "Admin");

            //za prof
            var teachers = await db.Teachers
                .Where(t => !string.IsNullOrWhiteSpace(t.Email))
                .ToListAsync();

            foreach (var t in teachers)
            {
                var email = t.Email.Trim().ToLower();
                var existing = await userManager.FindByEmailAsync(email);

                if (existing == null)
                {
                    var u = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        TeacherId = t.Id
                    };

                    var res = await userManager.CreateAsync(u, "Temp123!");
                    if (res.Succeeded)
                        await userManager.AddToRoleAsync(u, "Teacher");
                }
                else
                {
                    existing.TeacherId = t.Id;
                    await userManager.UpdateAsync(existing);

                    if (!await userManager.IsInRoleAsync(existing, "Teacher"))
                        await userManager.AddToRoleAsync(existing, "Teacher");
                }
            }

            //za stdenti, se generira prvo mail pa posle za login
            var students = await db.Students
                .Where(s => !string.IsNullOrWhiteSpace(s.FirstName) && !string.IsNullOrWhiteSpace(s.LastName))
                .ToListAsync();

            foreach (var s in students)
            {
                // anapetrova@student.edu.com
                var email = (s.FirstName + s.LastName)
                    .ToLower()
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Replace("'", "")
                    + "@student.edu.com";

                var existing = await userManager.FindByEmailAsync(email);

                if (existing == null)
                {
                    var u = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        StudentId = s.Id
                    };

                    var res = await userManager.CreateAsync(u, "student123");
                    if (res.Succeeded)
                        await userManager.AddToRoleAsync(u, "Student");
                }
                else
                {
                    if (existing.StudentId == null)
                    {
                        existing.StudentId = s.Id;
                        await userManager.UpdateAsync(existing);
                    }

                    if (!await userManager.IsInRoleAsync(existing, "Student"))
                        await userManager.AddToRoleAsync(existing, "Student");
                }
            }
        }
    }
}
