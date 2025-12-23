using System;
using System.ComponentModel.DataAnnotations;

namespace AcademicManagementSystem.Models
{
    public class Course
    {
        [Key]
        public int Id { get; set; } //primary key

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public int Credits { get; set; }

        [Required]
        public int Semester { get; set; }

        [StringLength(100)]
        public string? Programme { get; set; } // studiska programa kako sto e kti,ksiar..

        [StringLength(25)]
        public string? EducationLevel { get; set; } 

        public int? FirstTeacherId { get; set; } //fk kon Teacher

        public int? SecondTeacherId { get; set; } //fk kon Teacher

        // Eden predmet moze da ima povekje zapisi vo Enrollment
        // relacija many to many megju Student i Course preku Enrollment
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

        //eden predmet moe da ima i 2 profesori zato imame one to many relacija
        //first teacher objekt po firstTeacherId se naodja i vaka go dava celiot objekt t.e site podatoci
        public Teacher? FirstTeacher { get; set; }

        //second teacher
        public Teacher? SecondTeacher { get; set; }

    }
}
