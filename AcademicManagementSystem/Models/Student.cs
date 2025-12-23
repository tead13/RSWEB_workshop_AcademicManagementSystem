//using System.Runtime.CompilerServices;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcademicManagementSystem.Models
{
    public class Student
        //modelot e vsusnot klasa sto opisuva sto ek cuvame vo bazata
    {
        [Key]
        public long Id { get; set; } //primary key
        
        [Required]
        [StringLength(10)]
        public string StudentId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;
        //site prethodni se not nullable i zatoa se so string.Empty za da inicijalno e prazen string
        
        [DataType(DataType.Date)]
        
        public DateTime? EnrollmentDate { get; set; }
        
        public int? AcquiredCredits { get; set; } 
        
        public int? CurrentSemestar { get; set; }
        
        [StringLength(25)]
        public string? EducationLevel { get; set; } //Bachelor, Master, PhD

        //Eden student moze da ima povekje zapisi vo Enrollment
        //relacija many to many megju Student i Course preku Enrollment
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

        //ke gi spojam first i last name za polesno rpebaruvanje
        public string FullName => FirstName + " " + LastName;

        public string? ImageUrl { get; set; }

        [NotMapped]
        public IFormFile? ProfileImage { get; set; }
    }
}
