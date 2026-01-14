using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AcademicManagementSystem.Data;
using AcademicManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Alias to avoid conflict with AcademicManagementSystem.Areas.Teacher namespace
using TeacherModel = AcademicManagementSystem.Models.Teacher;

namespace AcademicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class TeachersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        private const string TeacherEmailDomain = "@profesor.edu.com";
        private const string TempTeacherPassword = "Teacher123!";

        public TeachersController(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin/Teachers
        public async Task<IActionResult> Index(string firstName, string lastName, string degree, string academicRank)
        {
            var teachers = _context.Teachers.AsQueryable();

            if (!string.IsNullOrEmpty(firstName))
                teachers = teachers.Where(t => t.FirstName.Contains(firstName));

            if (!string.IsNullOrEmpty(lastName))
                teachers = teachers.Where(t => t.LastName.Contains(lastName));

            if (!string.IsNullOrEmpty(degree))
                teachers = teachers.Where(t => t.Degree != null && t.Degree.Contains(degree));

            if (!string.IsNullOrEmpty(academicRank))
                teachers = teachers.Where(t => t.AcademicRank != null && t.AcademicRank.Contains(academicRank));

            return View(await teachers.ToListAsync());
        }

        // GET: Admin/Teachers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Id == id);
            if (teacher == null) return NotFound();

            var courses = await _context.Courses
                .Where(c => c.FirstTeacherId == id || c.SecondTeacherId == id)
                .ToListAsync();

            ViewBag.Courses = courses;

            return View(teacher);
        }

        // GET: Admin/Teachers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Teachers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TeacherModel teacher)
        {
            // Extra validation: email must end with @profesor.edu.com
            if (!string.IsNullOrWhiteSpace(teacher.Email))
            {
                var email = teacher.Email.Trim().ToLower();
                if (!email.EndsWith(TeacherEmailDomain))
                    ModelState.AddModelError(nameof(teacher.Email), $"Email мора да завршува на {TeacherEmailDomain}");
            }

            if (!ModelState.IsValid)
                return View(teacher);

            // ===== PROFILE IMAGE UPLOAD =====
            if (teacher.ProfileImage != null && teacher.ProfileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "teachers");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid() + Path.GetExtension(teacher.ProfileImage.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await teacher.ProfileImage.CopyToAsync(stream);

                teacher.ImageUrl = "/uploads/teachers/" + fileName;
            }

            // Save Teacher to DB first (so we have teacher.Id)
            _context.Add(teacher);
            await _context.SaveChangesAsync();

            // ===== CREATE/UPDATE IDENTITY USER FOR THIS TEACHER =====
            await EnsureTeacherRoleAsync();

            var normalizedEmail = teacher.Email?.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(normalizedEmail))
            {
                var user = await _userManager.FindByEmailAsync(normalizedEmail);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = normalizedEmail,
                        Email = normalizedEmail,
                        EmailConfirmed = true,
                        TeacherId = teacher.Id
                    };

                    var createResult = await _userManager.CreateAsync(user, TempTeacherPassword);
                    if (!createResult.Succeeded)
                    {
                        foreach (var err in createResult.Errors)
                            ModelState.AddModelError("", err.Description);

                        // Rollback Teacher (optional). For speed we just show errors.
                        return View(teacher);
                    }

                    await _userManager.AddToRoleAsync(user, "Teacher");

                    TempData["TempPassword"] =
                        $"Teacher login created: {normalizedEmail}  Password: {TempTeacherPassword}";
                }
                else
                {
                    // user exists -> ensure role + link teacher id
                    user.TeacherId = teacher.Id;
                    await _userManager.UpdateAsync(user);

                    if (!await _userManager.IsInRoleAsync(user, "Teacher"))
                        await _userManager.AddToRoleAsync(user, "Teacher");
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Teachers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null) return NotFound();

            return View(teacher);
        }

        // POST: Admin/Teachers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TeacherModel teacher)
        {
            if (id != teacher.Id) return NotFound();

            // Extra validation: email must end with @profesor.edu.com
            if (!string.IsNullOrWhiteSpace(teacher.Email))
            {
                var email = teacher.Email.Trim().ToLower();
                if (!email.EndsWith(TeacherEmailDomain))
                    ModelState.AddModelError(nameof(teacher.Email), $"Email мора да завршува на {TeacherEmailDomain}");
            }

            if (!ModelState.IsValid)
                return View(teacher);

            try
            {
                // ===== PROFILE IMAGE UPLOAD (IF NEW IMAGE SELECTED) =====
                if (teacher.ProfileImage != null && teacher.ProfileImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "teachers");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = Guid.NewGuid() + Path.GetExtension(teacher.ProfileImage.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await teacher.ProfileImage.CopyToAsync(stream);

                    teacher.ImageUrl = "/uploads/teachers/" + fileName;
                }
                else
                {
                    // keep existing image
                    var existingTeacher = await _context.Teachers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == teacher.Id);

                    if (existingTeacher != null)
                        teacher.ImageUrl = existingTeacher.ImageUrl;
                }

                _context.Update(teacher);
                await _context.SaveChangesAsync();

                // Optionally sync Identity email/TeacherId if email changed
                await EnsureTeacherRoleAsync();

                var normalizedEmail = teacher.Email?.Trim().ToLower();
                if (!string.IsNullOrWhiteSpace(normalizedEmail))
                {
                    var user = await _userManager.FindByEmailAsync(normalizedEmail);
                    if (user != null)
                    {
                        user.TeacherId = teacher.Id;
                        await _userManager.UpdateAsync(user);

                        if (!await _userManager.IsInRoleAsync(user, "Teacher"))
                            await _userManager.AddToRoleAsync(user, "Teacher");
                    }
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TeacherExists(teacher.Id))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Teachers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var teacher = await _context.Teachers.FirstOrDefaultAsync(m => m.Id == id);
            if (teacher == null) return NotFound();

            return View(teacher);
        }

        // POST: Admin/Teachers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher != null)
                _context.Teachers.Remove(teacher);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TeacherExists(int id)
        {
            return _context.Teachers.Any(e => e.Id == id);
        }

        private async Task EnsureTeacherRoleAsync()
        {
            if (!await _roleManager.RoleExistsAsync("Teacher"))
                await _roleManager.CreateAsync(new IdentityRole("Teacher"));
        }
    }
}
