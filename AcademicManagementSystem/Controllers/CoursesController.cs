using System.Linq;
using System.Threading.Tasks;
using AcademicManagementSystem.Data;
using AcademicManagementSystem.Models;
using AcademicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcademicManagementSystem.Controllers
{
    //[Authorize(Roles = "Admin")] 
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Courses
        public async Task<IActionResult> Index(string title, int? semester, string programme, int? teacherId)
        {
            var courses = _context.Courses
                .Include(c => c.FirstTeacher)
                .Include(c => c.SecondTeacher)
                .AsQueryable();

            if (!string.IsNullOrEmpty(title))
                courses = courses.Where(c => c.Title.Contains(title));

            if (semester.HasValue)
                courses = courses.Where(c => c.Semester == semester);

            if (!string.IsNullOrEmpty(programme))
                courses = courses.Where(c => c.Programme.Contains(programme));

            if (teacherId.HasValue)
                courses = courses.Where(c => c.FirstTeacherId == teacherId || c.SecondTeacherId == teacherId);

            ViewBag.Teachers = await _context.Teachers.ToListAsync();
            ViewBag.SelectedTeacherId = teacherId;

            return View(await courses.ToListAsync());
        }

        // GET: Courses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.FirstTeacher)
                .Include(c => c.SecondTeacher)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null) return NotFound();

            return View(course);
        }

        // GET: Courses/Create
        public IActionResult Create()
        {
            ViewData["FirstTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName");
            ViewData["SecondTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName");
            return View();
        }

        // POST: Courses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Credits,Semester,Programme,EducationLevel,FirstTeacherId,SecondTeacherId")] Course course)
        {
            if (!ModelState.IsValid)
            {
                ViewData["FirstTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName", course.FirstTeacherId);
                ViewData["SecondTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName", course.SecondTeacherId);
                return View(course);
            }

            _context.Add(course);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Courses/Edit/5
        public IActionResult Edit(int id)
        {
            var course = _context.Courses.FirstOrDefault(c => c.Id == id);
            if (course == null) return NotFound();

            ViewData["FirstTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName", course.FirstTeacherId);
            ViewData["SecondTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName", course.SecondTeacherId);

            var model = new EditCourseVM
            {
                CourseId = course.Id,
                Title = course.Title,
                Credits = course.Credits,
                Semester = course.Semester,
                Programme = course.Programme,
                EducationLevel = course.EducationLevel,
                FirstTeacherId = course.FirstTeacherId,
                SecondTeacherId = course.SecondTeacherId
            };

            return View(model);
        }

        // POST: Courses/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditCourseVM model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["FirstTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName", model.FirstTeacherId);
                ViewData["SecondTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName", model.SecondTeacherId);
                return View(model);
            }

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == model.CourseId);
            if (course == null) return NotFound();

            course.Title = model.Title;
            course.Credits = model.Credits;
            course.Semester = model.Semester;
            course.Programme = model.Programme;
            course.EducationLevel = model.EducationLevel;
            course.FirstTeacherId = model.FirstTeacherId;
            course.SecondTeacherId = model.SecondTeacherId;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Courses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.FirstTeacher)
                .Include(c => c.SecondTeacher)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null) return NotFound();

            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Optional: filter by teacher
        public async Task<IActionResult> ByTeacher(int teacherId)
        {
            var courses = await _context.Courses
                .Include(c => c.FirstTeacher)
                .Include(c => c.SecondTeacher)
                .Where(c => c.FirstTeacherId == teacherId || c.SecondTeacherId == teacherId)
                .ToListAsync();

            return View(courses);
        }
    }
}
