using System;
using System.Linq;
using System.Threading.Tasks;
using AcademicManagementSystem.Data;
using AcademicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademicManagementSystem.Areas.Teacher.Controllers
{
    [Area("Teacher")]
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public TeacherController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Teacher/Teacher
        public async Task<IActionResult> Index()
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return Forbid();

            var courses = await _context.Courses
                .Where(c => c.FirstTeacherId == teacher.Id || c.SecondTeacherId == teacher.Id)
                .OrderBy(c => c.Title)
                .Select(c => new TeacherCourseListItemVM
                {
                    CourseId = c.Id,
                    Title = c.Title,
                    Semester = c.Semester,
                    Programme = c.Programme
                })
                .ToListAsync();

            return View(courses);
        }

        // GET: /Teacher/Teacher/Course/5?year=2024
        public async Task<IActionResult> Course(int id, int? year)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return Forbid();

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);
            if (course == null) return NotFound();

            if (!(course.FirstTeacherId == teacher.Id || course.SecondTeacherId == teacher.Id))
                return Forbid();

            var years = await _context.Enrollments
                .Where(e => e.CourseId == id)
                .Select(e => e.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            int selectedYear = year ?? (years.FirstOrDefault() == 0 ? DateTime.Now.Year : years.FirstOrDefault());

            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.CourseId == id && e.Year == selectedYear)
                .OrderBy(e => e.Student.LastName)
                .ThenBy(e => e.Student.FirstName)
                .Select(e => new TeacherEnrollmentRowVM
                {
                    CourseId = e.CourseId,
                    StudentId = e.StudentId,
                    Year = e.Year,

                    StudentIndex = e.Student.Index,
                    StudentFullName = e.Student.FirstName + " " + e.Student.LastName,

                    Semester = e.Semester,
                    Points = e.Points,
                    Grade = e.Grade,
                    FinishDate = e.FinishDate,

                    SeminarUrl = e.SeminarUrl,
                    ProjectUrl = e.ProjectUrl,
                    ExamUrl = e.ExamUrl,

                    IsActive = e.FinishDate == null
                })
                .ToListAsync();

            var vm = new TeacherCourseDetailsVM
            {
                CourseId = course.Id,
                Title = course.Title,
                SelectedYear = selectedYear,
                AvailableYears = years,
                Enrollments = enrollments
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> EditEnrollment(int courseId, int studentId, int year)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return Forbid();

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null) return NotFound();

            if (!(course.FirstTeacherId == teacher.Id || course.SecondTeacherId == teacher.Id))
                return Forbid();

            var e = await _context.Enrollments
                .Include(x => x.Student)
                .FirstOrDefaultAsync(x => x.CourseId == courseId && x.StudentId == studentId && x.Year == year);

            if (e == null) return NotFound();
            if (e.FinishDate != null) return Forbid(); // only active

            var vm = new TeacherEnrollmentEditVM
            {
                CourseId = e.CourseId,
                StudentId = e.StudentId,
                Year = e.Year,

                StudentIndex = e.Student.Index,
                StudentFullName = e.Student.FirstName + " " + e.Student.LastName,

                Points = e.Points,
                Grade = e.Grade,
                FinishDate = e.FinishDate,

                SeminarUrl = e.SeminarUrl,
                ProjectUrl = e.ProjectUrl,
                ExamUrl = e.ExamUrl
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEnrollment(TeacherEnrollmentEditVM vm)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return Forbid();

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == vm.CourseId);
            if (course == null) return NotFound();

            if (!(course.FirstTeacherId == teacher.Id || course.SecondTeacherId == teacher.Id))
                return Forbid();

            var e = await _context.Enrollments
                .FirstOrDefaultAsync(x => x.CourseId == vm.CourseId && x.StudentId == vm.StudentId && x.Year == vm.Year);

            if (e == null) return NotFound();
            if (e.FinishDate != null) return Forbid();

            if (!ModelState.IsValid)
                return View(vm);

            e.Points = vm.Points;
            e.Grade = vm.Grade;
            e.FinishDate = vm.FinishDate;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Course), new { id = vm.CourseId, year = vm.Year });
        }

        private async Task<AcademicManagementSystem.Models.Teacher?> GetCurrentTeacherAsync()
        {
            // 1) ако имаш Teacher.ApplicationUserId
            var userId = _userManager.GetUserId(User);
            var t = await _context.Teachers.FirstOrDefaultAsync(x => x.ApplicationUserId == userId);
            if (t != null) return t;

            // 2) fallback ако имаш Teacher.Email
            var email = User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(email))
                return await _context.Teachers.FirstOrDefaultAsync(x => x.Email == email);

            return null;
        }
    }
}
