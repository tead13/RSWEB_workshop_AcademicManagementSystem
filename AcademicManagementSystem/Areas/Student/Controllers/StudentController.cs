using System.Linq;
using System.Threading.Tasks;
using AcademicManagementSystem.Data;
using AcademicManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;


namespace AcademicManagementSystem.Areas.Student.Controllers
{
    [Area("Student")]
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Student/Student
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.StudentId == null)
                return Forbid();

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == user.StudentId.Value);

            if (student == null)
                return Forbid();

            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.StudentId == student.Id)
                .OrderByDescending(e => e.Year)
                .ToListAsync();

            ViewBag.StudentName = $"{student.FirstName} {student.LastName}";
            return View(enrollments);
        }

        // GET: /Student/Student/Course/5  (EnrollmentId)
        public async Task<IActionResult> Course(long id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.StudentId == null)
                return Forbid();

            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.FirstTeacher)
                .Include(e => e.Course)
                .ThenInclude(c => c.SecondTeacher)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enrollment == null)
                return NotFound();

            if (enrollment.StudentId != user.StudentId.Value)
                return Forbid();

            return View(enrollment);
        }

        // POST: upload seminar file (doc/docx/pdf)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadSeminar(long id, IFormFile seminarFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.StudentId == null) return Forbid();

            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enrollment == null) return NotFound();
            if (enrollment.StudentId != user.StudentId.Value) return Forbid();

            if (seminarFile == null || seminarFile.Length == 0)
            {
                TempData["Error"] = "Please choose a file.";
                return RedirectToAction("Course", new { area = "Student", id });
            }

            // max 10MB
            if (seminarFile.Length > 10 * 1024 * 1024)
            {
                TempData["Error"] = "File too large (max 10MB).";
                return RedirectToAction("Course", new { area = "Student", id });
            }

            var ext = Path.GetExtension(seminarFile.FileName).ToLowerInvariant();
            var allowed = new[] { ".pdf", ".doc", ".docx" };
            if (!allowed.Contains(ext))
            {
                TempData["Error"] = "Only .pdf, .doc, .docx are allowed.";
                return RedirectToAction("Course", new { area = "Student", id });
            }

            // delete old file if exists
            if (!string.IsNullOrWhiteSpace(enrollment.SeminarUrl) && enrollment.SeminarUrl.StartsWith("/uploads/seminars/"))
            {
                var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", enrollment.SeminarUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            var safeIndex = enrollment.StudentId.ToString();
            var safeCourse = enrollment.CourseId.ToString();
            var fileName = $"seminar_{safeCourse}_{safeIndex}_{enrollment.Year}_{Guid.NewGuid():N}{ext}";
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "seminars");
            Directory.CreateDirectory(folder);

            var fullPath = Path.Combine(folder, fileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await seminarFile.CopyToAsync(stream);
            }

            var original = Path.GetFileName(seminarFile.FileName); //samo ime bez path
            var encoded = Uri.EscapeDataString(original);
            enrollment.SeminarUrl = "/uploads/seminars/" + fileName + "?name=" + encoded;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Seminar uploaded.";
            return RedirectToAction("Course", new { area = "Student", id });
        }

        // POST: delete seminar file
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSeminar(long id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.StudentId == null) return Forbid();

            var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.Id == id);
            if (enrollment == null) return NotFound();
            if (enrollment.StudentId != user.StudentId.Value) return Forbid();

            if (!string.IsNullOrWhiteSpace(enrollment.SeminarUrl) && enrollment.SeminarUrl.StartsWith("/uploads/seminars/"))
            {
                var urlPath = enrollment.SeminarUrl.Split('?')[0]; // тргни ?name=...
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    urlPath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }

            enrollment.SeminarUrl = null;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Seminar deleted.";
            return RedirectToAction("Course", new { area = "Student", id });
        }

        // POST: update project url (GitHub)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProjectUrl(long id, string projectUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.StudentId == null) return Forbid();

            var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.Id == id);
            if (enrollment == null) return NotFound();
            if (enrollment.StudentId != user.StudentId.Value) return Forbid();

            projectUrl = (projectUrl ?? "").Trim();

            // allow empty (means remove)
            if (!string.IsNullOrEmpty(projectUrl))
            {
                // basic validation: must look like a URL, and preferably github
                if (!Uri.TryCreate(projectUrl, UriKind.Absolute, out var uri))
                {
                    TempData["Error"] = "Invalid URL.";
                    return RedirectToAction("Course", new { area = "Student", id });
                }
                // optional: force github
                if (!uri.Host.Contains("github.com"))
                {
                    TempData["Error"] = "Project URL must be a GitHub link.";
                    return RedirectToAction("Course", new { area = "Student", id });
                }
            }

            enrollment.ProjectUrl = string.IsNullOrEmpty(projectUrl) ? null : projectUrl;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Project URL saved.";
            return RedirectToAction("Course", new { area = "Student", id });
        }

        // POST: delete project url
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProjectUrl(long id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.StudentId == null) return Forbid();

            var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.Id == id);
            if (enrollment == null) return NotFound();
            if (enrollment.StudentId != user.StudentId.Value) return Forbid();

            enrollment.ProjectUrl = null;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Project URL deleted.";
            return RedirectToAction("Course", new { area = "Student", id });
        }

    }
}
