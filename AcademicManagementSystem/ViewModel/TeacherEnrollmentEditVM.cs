using System;
using System.ComponentModel.DataAnnotations;

namespace AcademicManagementSystem.ViewModels
{
    public class TeacherEnrollmentEditVM
    {
        public int CourseId { get; set; }
        public int StudentId { get; set; }
        public int Year { get; set; }

        public string StudentIndex { get; set; } = "";
        public string StudentFullName { get; set; } = "";

        [Range(0, 100)]
        public int? Points { get; set; }

        [Range(5, 10)]
        public int? Grade { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FinishDate { get; set; }

        public string? SeminarUrl { get; set; }
        public string? ProjectUrl { get; set; }
        public string? ExamUrl { get; set; }
    }
}
