using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcademicManagementSystem.Models
{
    public class Teacher
    {
        [Key]
        public int Id { get; set; } //primary key
                
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Degree { get; set; } 

        [StringLength(25)]
        public string? AcademicRank { get; set; }

        [StringLength(10)]
        public string? OfficeNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? HireDate { get; set; }

        // Predmeti kade e prv profesor, moze da bide prof na povekje predmeti
        public ICollection<Course> FirstTeacherCourses { get; set; } = new List<Course>();

        // Predmeti e vtor profesor, isto moze da bide na povekje predmeti
        public ICollection<Course> SecondTeacherCourses { get; set; } = new List<Course>();

        //ke gi spojam first i last name za polesno rpebaruvanje
        public string FullName => FirstName + " " + LastName;

        public string? ImageUrl { get; set; }

        [NotMapped]
        public IFormFile? ProfileImage { get; set; }


    }
}
