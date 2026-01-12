using AcademicManagementSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace AcademicManagementSystem.ViewModels
{
    public class ManageCourseEnrollmentsVM
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;

        [Required]
        public int Year { get; set; }

        [Required]
        public string Semester { get; set; } = "Winter"; // Winter/Summer

        public List<StudentPickVM> Students { get; set; } = new();
        public List<EnrolledStudentVM> Enrolled { get; set; } = new();

        [DataType(DataType.Date)]
        public DateTime? FinishDate { get; set; }
    }

    public class StudentPickVM
    {
        public long StudentId { get; set; }
        public string StudentIndex { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool Selected { get; set; }
    }

    public class EnrolledStudentVM
    {
        public long EnrollmentId { get; set; }
        public long StudentId { get; set; }
        public string StudentIndex { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public EnrollmentStatus Status { get; set; }
        public DateTime? FinishDate { get; set; }
        public bool Selected { get; set; }
    }
}
