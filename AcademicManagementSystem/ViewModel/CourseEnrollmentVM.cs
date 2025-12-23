using AcademicManagementSystem.Models;

namespace AcademicManagementSystem.ViewModels
{
    public class CourseEnrollmentVM
    {
        public long StudentId { get; set; }

        // Ime + prezime, samo za prikaz
        public string FullName { get; set; } = string.Empty;

        // dali studentot e zapisan na predmetot
        public bool IsEnrolled { get; set; }

        // Enrollment podatoci (se menuvaat vo Edit Course)
        public EnrollmentStatus Status { get; set; }
        public int? Grade { get; set; }
        public bool IsRepeating { get; set; }
    }
}
