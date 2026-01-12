using System;
using System.Linq;
using System.Threading.Tasks;
using AcademicManagementSystem.Data;
using AcademicManagementSystem.Models;
using AcademicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcademicManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Courses
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

            ViewBag.Teachers = _context.Teachers.ToList();
            ViewBag.SelectedTeacherId = teacherId;

            return View(await courses.ToListAsync());
        }

        // GET: Admin/Courses/Details/5
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

        // GET: Admin/Courses/Create
        public IActionResult Create()
        {
            ViewData["FirstTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName");
            ViewData["SecondTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName");
            return View();
        }

        // POST: Admin/Courses/Create
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

        // GET: Admin/Courses/Edit/5
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

        // POST: Admin/Courses/Edit
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

        // GET: Admin/Courses/Delete/5
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

        // POST: Admin/Courses/Delete/5
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

        public async Task<IActionResult> ByTeacher(int teacherId)
        {
            var courses = await _context.Courses
                .Include(c => c.FirstTeacher)
                .Include(c => c.SecondTeacher)
                .Where(c => c.FirstTeacherId == teacherId || c.SecondTeacherId == teacherId)
                .ToListAsync();

            return View(courses);
        }

        // =========================
        // ADMIN: Manage Enrollments
        // =========================

        [HttpGet]
        public async Task<IActionResult> ManageEnrollments(int id, int? year, string? semester)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);
            if (course == null) return NotFound();

            int y = year ?? DateTime.Now.Year;
            string sem = string.IsNullOrWhiteSpace(semester) ? "Winter" : semester;

            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.CourseId == id && e.Year == y && e.Semester == sem)
                .ToListAsync();

            var allStudents = await _context.Students
                .OrderBy(s => s.StudentId)
                .ToListAsync();

            var vm = new ManageCourseEnrollmentsVM
            {
                CourseId = course.Id,
                CourseTitle = course.Title,
                Year = y,
                Semester = sem,
                Students = allStudents.Select(s => new StudentPickVM
                {
                    StudentId = s.Id,
                    StudentIndex = s.StudentId,
                    FullName = s.FullName,
                    Selected = false
                }).ToList(),
                Enrolled = enrollments.Select(e => new EnrolledStudentVM
                {
                    EnrollmentId = e.Id,
                    StudentId = e.StudentId,
                    StudentIndex = e.Student.StudentId,
                    FullName = e.Student.FullName,
                    Status = e.Status,
                    FinishDate = e.FinishDate,
                    Selected = false
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollSelected(ManageCourseEnrollmentsVM vm)
        {
            var selectedStudentIds = vm.Students.Where(s => s.Selected).Select(s => s.StudentId).ToList();
            if (!selectedStudentIds.Any())
                return RedirectToAction(nameof(ManageEnrollments), new { id = vm.CourseId, year = vm.Year, semester = vm.Semester });

            var existing = await _context.Enrollments
                .Where(e => e.CourseId == vm.CourseId && e.Year == vm.Year && e.Semester == vm.Semester)
                .Select(e => e.StudentId)
                .ToListAsync();

            var toAdd = selectedStudentIds.Except(existing).ToList();

            foreach (var sid in toAdd)
            {
                _context.Enrollments.Add(new Enrollment
                {
                    CourseId = vm.CourseId,
                    StudentId = sid,
                    Year = vm.Year,
                    Semester = vm.Semester,
                    Status = EnrollmentStatus.Enrolled,
                    EnrolledOn = DateTime.Now,
                    IsRepeating = false
                    // сите други полиња остануваат null/default
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageEnrollments), new { id = vm.CourseId, year = vm.Year, semester = vm.Semester });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateSelected(ManageCourseEnrollmentsVM vm)
        {
            //ako ne izbere datom da se stavi denesen
            var finish = vm.FinishDate ?? DateTime.Today;

            var selectedEnrollmentIds = vm.Enrolled.Where(e => e.Selected).Select(e => e.EnrollmentId).ToList();
            if (!selectedEnrollmentIds.Any())
                return RedirectToAction(nameof(ManageEnrollments), new { id = vm.CourseId, year = vm.Year, semester = vm.Semester });

            var enrollments = await _context.Enrollments
                .Where(e => selectedEnrollmentIds.Contains(e.Id))
                .ToListAsync();

            foreach (var e in enrollments)
            {
                e.FinishDate = finish;
                e.Status = EnrollmentStatus.Dropped; // деактивиран/отпишан
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageEnrollments), new { id = vm.CourseId, year = vm.Year, semester = vm.Semester });
        }
    }
}
