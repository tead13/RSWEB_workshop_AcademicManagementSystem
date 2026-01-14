using System;
using System.ComponentModel.DataAnnotations;

namespace AcademicManagementSystem.ViewModels
{
    public class TeacherEnrollmentEditVM
    {
        public long EnrollmentId { get; set; }
        public int CourseId { get; set; }
        public int Year { get; set; } // za da moze da se vrti nazad na istata godina

        // samo da moze da gi prikaze bez da moze da menuva
        public string StudentIndex { get; set; } = "";
        public string StudentFullName { get; set; } = "";
        public string? SeminarUrl { get; set; }
        public string? ProjectUrl { get; set; }

        // toa sto moze da menuva profesorot
        [Range(0, 100)]
        public int? SeminarPoints { get; set; }

        [Range(0, 100)]
        public int? ProjectPoints { get; set; }

        [Range(0, 100)]
        public int? ExamPoints { get; set; }
                
        [Range(5, 10)]
        public int? Grade { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FinishDate { get; set; }

    }
}
