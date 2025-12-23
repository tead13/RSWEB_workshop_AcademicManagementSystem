using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcademicManagementSystem.Data;
using AcademicManagementSystem.Models;
using AcademicManagementSystem.ViewModels;

namespace AcademicManagementSystem.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Courses
        /*star index bez dropdownz za teachers
        public async Task<IActionResult> Index(
            string title,
            int? semester,
            string programme )

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
                courses = courses.Where(c => c.Programme == programme);

            return View(await courses.ToListAsync());
        }*/

        // GET: Courses
        public async Task<IActionResult> Index(
            string title,
            int? semester,
            string programme,
            int? teacherId)
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
                courses = courses.Where(c =>
                    c.FirstTeacherId == teacherId ||
                    c.SecondTeacherId == teacherId);

            ViewBag.Teachers = _context.Teachers.ToList();
            ViewBag.SelectedTeacherId = teacherId;

            return View(await courses.ToListAsync());
        }


        // GET: Courses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.FirstTeacher)
                .Include(c => c.SecondTeacher)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // GET: Courses/Create
        public IActionResult Create()
        {
            //namesto posebno da gi pisuvam ime i prezime, gi spoiuvam vo FullName property vo Teacher modelot
            //ako treba ova moze da se smeni i pak da se vrati posebno FirstName i LastName
            //ViewData["FirstTeacherId"] = new SelectList(_context.Teachers, "Id", "FirstName");
            //ViewData["SecondTeacherId"] = new SelectList(_context.Teachers, "Id", "FirstName");

            ViewData["FirstTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName");
            ViewData["SecondTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName");
            
            return View();
        }

        // POST: Courses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Id,Title,Credits,Semester,Programme,EducationLevel,FirstTeacherId,SecondTeacherId")] 
            Course course)
        {
            if (ModelState.IsValid)
            {
                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["FirstTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName", course.FirstTeacherId);
            ViewData["SecondTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName", course.SecondTeacherId);
            return View(course);
        }
        /* staro 
        // GET: Courses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            ViewData["FirstTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName", course.FirstTeacherId);
            ViewData["SecondTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName", course.SecondTeacherId);
            return View(course);
        }*/

        // GET: Courses/Edit/5
        public IActionResult Edit(int id)
        {
            var course = _context.Courses
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                .FirstOrDefault(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            ViewData["FirstTeacherId"] = new SelectList(
                _context.Teachers,
                "Id",
                "FullName",
                course.FirstTeacherId);

            ViewData["SecondTeacherId"] = new SelectList(
                _context.Teachers,
                "Id",
                "FullName",
                course.SecondTeacherId);


            var model = new EditCourseVM
            {
                CourseId = course.Id,
                Title = course.Title,
                Credits = course.Credits,
                Semester = course.Semester,
                Programme = course.Programme,
                EducationLevel = course.EducationLevel,
                FirstTeacherId = course.FirstTeacherId,
                SecondTeacherId = course.SecondTeacherId,

                Students = _context.Students
                        .AsEnumerable()
                        .Select(s =>
                        {
                            var enrollment = course.Enrollments
                                   .FirstOrDefault(e => e.StudentId == s.Id);

                            return new CourseEnrollmentVM
                            {
                                StudentId = s.Id,
                                FullName = s.FirstName + " " + s.LastName,

                                IsEnrolled = enrollment != null,
                                Status = enrollment != null
                                    ? enrollment.Status
                                    : EnrollmentStatus.Enrolled,
                                Grade = enrollment != null ? enrollment.Grade : null,
                                IsRepeating = enrollment != null ? enrollment.IsRepeating : false
                            };
                        })
                        .ToList()
            };

            return View(model); // nazad prakja EditCourseVM
        }

        // POST: Courses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        /* ova ni e pred 3b t.e tuka samo se menuva/editira course pr menuvame atributi kako title, credits...
         *nem anikakva vrska so student i enrollment t.e ne brise/dodava student na predmetot[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Credits,Semester,Programme,EducationLevel,FirstTeacherId,SecondTeacherId")] Course course)
        {
            if (id != course.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.Id))
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
            ViewData["FirstTeacherId"] = new SelectList(_context.Teachers, "Id", "FirstName", course.FirstTeacherId);
            ViewData["SecondTeacherId"] = new SelectList(_context.Teachers, "Id", "FirstName", course.SecondTeacherId);
            return View(course);
        }*/

        //tuka ke go stavam noviot POST Edit kade sto ke moze da se menuva i Course i Student i Enrollment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditCourseVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            //tuka se zema predmetot zaedno so enrollments
            var course = await _context.Courses
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == model.CourseId);

            if (course == null)
            {
                return NotFound();
            }

            //update na osnovnite Course polinja
            course.Title = model.Title;
            

            //tuka e celata logika za enrollment
            foreach (var s in model.Students)
            {
                var enrollment = course.Enrollments
                    .FirstOrDefault(e => e.StudentId == s.StudentId);

                //dodavanje nov enrollment
                if (s.IsEnrolled && enrollment == null)
                {
                    _context.Enrollments.Add(new Enrollment
                    {
                        CourseId = course.Id,
                        StudentId = s.StudentId,
                        Status = s.Status,
                        Grade = s.Grade,
                        IsRepeating = s.IsRepeating,
                        EnrolledOn = DateTime.Now
                    });
                }
                //brisenje enrollment
                else if (!s.IsEnrolled && enrollment != null)
                {
                    _context.Enrollments.Remove(enrollment);
                }
                //update na enrollment
                else if (enrollment != null)
                {
                    enrollment.Status = s.Status;
                    enrollment.Grade = s.Grade;
                    enrollment.IsRepeating = s.IsRepeating;
                }
            }

            //na kraj da go zacuvame vo baza
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // GET: Courses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.FirstTeacher)
                .Include(c => c.SecondTeacher)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (course == null)
            {
                return NotFound();
            }

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
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }

        public async Task<IActionResult> ByTeacher(int teacherId)
        {
            var courses = await _context.Courses
                .Include(c => c.FirstTeacher)
                .Include(c => c.SecondTeacher)
                .Where(c =>
                    c.FirstTeacherId == teacherId ||
                    c.SecondTeacherId == teacherId)
                .ToListAsync();

            return View(courses);
        }

    }
}
