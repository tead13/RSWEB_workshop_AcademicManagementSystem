using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcademicManagementSystem.Data;
using AcademicManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace AcademicManagementSystem.Controllers
{
    //[Authorize(Roles = "Admin")]    
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        public StudentsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Students
        public async Task<IActionResult> Index(
            string studentId,
            string firstName,
            string lastName)
        {
            var students = _context.Students.AsQueryable();

            if (!string.IsNullOrWhiteSpace(studentId))
                students = students.Where(s => s.StudentId.Contains(studentId));

            if (!string.IsNullOrWhiteSpace(firstName))
                students = students.Where(s => s.FirstName.Contains(firstName));

            if (!string.IsNullOrWhiteSpace(lastName))
                students = students.Where(s => s.LastName.Contains(lastName));

            return View(await students.ToListAsync());
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Course)
                        .ThenInclude(c => c.FirstTeacher)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Students/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student)
        {
            if (ModelState.IsValid)
            {
                // ===== PROFILE IMAGE UPLOAD =====
                if (student.ProfileImage != null && student.ProfileImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/students");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = Guid.NewGuid() + Path.GetExtension(student.ProfileImage.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await student.ProfileImage.CopyToAsync(stream);

                    student.ImageUrl = "/uploads/students/" + fileName;
                }

                _context.Students.Add(student);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(student);
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        // POST: Students/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, Student student)
        {
            if (id != student.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // ===== PROFILE IMAGE UPLOAD (IF NEW IMAGE SELECTED) =====
                    if (student.ProfileImage != null && student.ProfileImage.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/students");
                        Directory.CreateDirectory(uploadsFolder);

                        var fileName = Guid.NewGuid() + Path.GetExtension(student.ProfileImage.FileName);
                        var filePath = Path.Combine(uploadsFolder, fileName);

                        using var stream = new FileStream(filePath, FileMode.Create);
                        await student.ProfileImage.CopyToAsync(stream);

                        student.ImageUrl = "/uploads/students/" + fileName;
                    }
                    else
                    {
                        // keep existing image if no new one uploaded
                        var existingStudent = await _context.Students
                            .AsNoTracking()
                            .FirstOrDefaultAsync(s => s.Id == student.Id);

                        if (existingStudent != null)
                        {
                            student.ImageUrl = existingStudent.ImageUrl;
                        }
                    }

                    _context.Update(student);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            return View(student);
        }

        //funkcijata so koja ke napravistudentot prikacuvanje na seminarska
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadSeminar(
    long studentId,
    int courseId,
    IFormFile seminarFile)
        {
            if (seminarFile == null || seminarFile.Length == 0)
                return RedirectToAction("Details", new { id = studentId });

            var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
            var extension = Path.GetExtension(seminarFile.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
                return RedirectToAction("Details", new { id = studentId });

            
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "seminars");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid() + extension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await seminarFile.CopyToAsync(stream);
            }

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e =>
                    e.StudentId == studentId &&
                    e.CourseId == courseId);

            if (enrollment != null)
            {
                enrollment.SeminarUrl = "/uploads/seminars/" + uniqueFileName;
                enrollment.SeminarFileName = seminarFile.FileName;
                enrollment.SeminarUploadedAt = DateTime.Now;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = studentId });
        }

        // da moze da se izbrise prikacenata seminarska
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSeminar(long studentId, int courseId)
        {
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e =>
                    e.StudentId == studentId &&
                    e.CourseId == courseId);

            if (enrollment != null && !string.IsNullOrEmpty(enrollment.SeminarUrl))
            {
                //fizicki se brise fajlot
                var filePath = Path.Combine(
                    _env.WebRootPath,
                    enrollment.SeminarUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
                );

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                //brisenje od baza
                enrollment.SeminarUrl = null;
                enrollment.SeminarFileName = null;
                enrollment.SeminarUploadedAt = null;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = studentId });
        }



        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(long id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}
