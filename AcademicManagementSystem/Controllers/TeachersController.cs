using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcademicManagementSystem.Data;
using AcademicManagementSystem.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;

namespace AcademicManagementSystem.Controllers
{
    //[Authorize(Roles = "Admin")]
    public class TeachersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public TeachersController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Teachers
        public async Task<IActionResult> Index(
            string firstName,
            string lastName,
            string degree,
            string academicRank)
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

        // GET: Teachers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //da se prikazat i predmetite na koi predava nastavnikot
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
            {
                return NotFound();
            }

            var courses = await _context.Courses
                .Where(c => c.FirstTeacherId == id || c.SecondTeacherId == id)
                .ToListAsync();

            ViewBag.Courses = courses;

            return View(teacher);
        }

        // GET: Teachers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Teachers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Teacher teacher)
        {
            if (ModelState.IsValid)
            {
                // ===== PROFILE IMAGE UPLOAD =====
                if (teacher.ProfileImage != null && teacher.ProfileImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/teachers");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = Guid.NewGuid() + Path.GetExtension(teacher.ProfileImage.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await teacher.ProfileImage.CopyToAsync(stream);

                    teacher.ImageUrl = "/uploads/teachers/" + fileName;
                }

                _context.Add(teacher);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(teacher);
        }

        // GET: Teachers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null)
            {
                return NotFound();
            }
            return View(teacher);
        }

        // POST: Teachers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Teacher teacher)
        {
            if (id != teacher.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // ===== PROFILE IMAGE UPLOAD (IF NEW IMAGE SELECTED) =====
                    if (teacher.ProfileImage != null && teacher.ProfileImage.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/teachers");
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
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TeacherExists(teacher.Id))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(teacher);
        }
        // GET: Teachers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (teacher == null)
            {
                return NotFound();
            }

            return View(teacher);
        }

        // POST: Teachers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher != null)
            {
                _context.Teachers.Remove(teacher);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TeacherExists(int id)
        {
            return _context.Teachers.Any(e => e.Id == id);
        }
    }
}
