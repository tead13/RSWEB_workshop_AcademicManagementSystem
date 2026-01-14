using System;
using System.Linq;
using System.Threading.Tasks;
using AcademicManagementSystem.Data;
using AcademicManagementSystem.Models;
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
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Teacher/Teacher
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.TeacherId == null)
                return Forbid();

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.Id == user.TeacherId.Value);

            if (teacher == null)
                return Forbid();

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

            var vm = new TeacherHomeVM
            {
                TeacherFullName = $"{teacher.FirstName} {teacher.LastName}",
                Courses = courses
            };

            return View(vm);
        }

        // GET: /Teacher/Teacher/Course/1?year=2026
        public async Task<IActionResult> Course(int id, int? year)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.TeacherId == null) return Forbid();
            int teacherId = user.TeacherId.Value;

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);
            if (course == null) return NotFound();

            //moze da gi videi samo smoite predmeti
            if (!(course.FirstTeacherId == teacherId || course.SecondTeacherId == teacherId))
                return Forbid();

            var years = await _context.Enrollments
                .Where(e => e.CourseId == id)
                .Select(e => e.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            int selectedYear = year ?? (years.Count > 0 ? years[0] : DateTime.Now.Year);

            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.CourseId == id && e.Year == selectedYear)
                .OrderBy(e => e.Student.LastName)
                .ThenBy(e => e.Student.FirstName)
                .Select(e => new TeacherEnrollmentRowVM
                {
                    EnrollmentId = e.Id,
                    CourseId = e.CourseId,
                    Year = e.Year,

                    StudentIndex = e.Student.StudentId,
                    StudentFullName = e.Student.FullName,

                    Semester = e.Semester,

                    SeminarUrl = e.SeminarUrl,
                    ProjectUrl = e.ProjectUrl,

                    SeminarPoints = e.SeminarPoints,
                    ProjectPoints = e.ProjectPoints,
                    ExamPoints = e.ExamPoints,

                    // Total points ke bidat prikazani "Points" i ke se zbir od site
                    Points = (e.ExamPoints ?? 0)
                           + (e.SeminarPoints ?? 0)
                           + (e.ProjectPoints ?? 0),

                    Grade = e.Grade,
                    FinishDate = e.FinishDate,

                    IsActive = e.FinishDate == null && e.Status == EnrollmentStatus.Enrolled
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

        // GET: /Teacher/Teacher/EditEnrollment/5
        public async Task<IActionResult> EditEnrollment(long id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.TeacherId == null) return Forbid();
            int teacherId = user.TeacherId.Value;

            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enrollment == null) return NotFound();

            //prof smee da menuva samo svoi predmeti
            if (!(enrollment.Course.FirstTeacherId == teacherId || enrollment.Course.SecondTeacherId == teacherId))
                return Forbid();

            var vm = new TeacherEnrollmentEditVM
            {
                EnrollmentId = enrollment.Id,
                CourseId = enrollment.CourseId,
                Year = enrollment.Year,

                StudentIndex = enrollment.Student.StudentId,
                StudentFullName = enrollment.Student.FirstName + " " + enrollment.Student.LastName,

                SeminarUrl = enrollment.SeminarUrl,
                ProjectUrl = enrollment.ProjectUrl,

                SeminarPoints = enrollment.SeminarPoints,
                ProjectPoints = enrollment.ProjectPoints,
                ExamPoints = enrollment.ExamPoints,
                
                Grade = enrollment.Grade,
                FinishDate = enrollment.FinishDate
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEnrollment(TeacherEnrollmentEditVM vm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.TeacherId == null) return Forbid();
            int teacherId = user.TeacherId.Value;

            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == vm.EnrollmentId);

            if (enrollment == null) return NotFound();

            if (!(enrollment.Course.FirstTeacherId == teacherId || enrollment.Course.SecondTeacherId == teacherId))
                return Forbid();

            if (!ModelState.IsValid)
                return View(vm);

            // Teacher updates
            enrollment.SeminarPoints = vm.SeminarPoints;
            enrollment.ProjectPoints = vm.ProjectPoints;
            enrollment.ExamPoints = vm.ExamPoints;
            
            enrollment.Grade = vm.Grade;
            enrollment.FinishDate = vm.FinishDate;

            // ako vneseme finish date - vise ne e "active"
            if (enrollment.FinishDate != null && enrollment.Status == EnrollmentStatus.Enrolled)
                enrollment.Status = EnrollmentStatus.Completed;

            await _context.SaveChangesAsync();

            //vrakjanje nazad na istiot predmet i ista godina izberena
            return RedirectToAction("Course", new { area = "Teacher", id = enrollment.CourseId, year = enrollment.Year });
        }
    }
}
