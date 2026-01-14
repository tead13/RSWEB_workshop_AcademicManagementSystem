using System;
using System.Collections.Generic;

namespace AcademicManagementSystem.ViewModels
{
    public class TeacherCourseDetailsVM
    {
        public int CourseId { get; set; }
        public string Title { get; set; } = "";
        public int SelectedYear { get; set; }
        public List<int> AvailableYears { get; set; } = new();
        public List<TeacherEnrollmentRowVM> Enrollments { get; set; } = new();
    }

    public class TeacherEnrollmentRowVM
    {
        public int CourseId { get; set; }
        public int StudentId { get; set; }
        public long EnrollmentId { get; set; }

        public int Year { get; set; }

        public string StudentIndex { get; set; } = "";
        public string StudentFullName { get; set; } = "";

        public string Semester { get; set; } = "";

        //poeni od site posebno
        public int? SeminarPoints { get; set; }
        public int? ProjectPoints { get; set; }
        public int? ExamPoints { get; set; }

        //vkuni poeni - zbir
        public int Points { get; set; }

        public int? Grade { get; set; }
        public DateTime? FinishDate { get; set; }

        public string? SeminarUrl { get; set; }
        public string? ProjectUrl { get; set; }

        public bool IsActive { get; set; }
    }
}
