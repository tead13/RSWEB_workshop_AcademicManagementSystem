using System;
using System.ComponentModel.DataAnnotations;

namespace AcademicManagementSystem.Models
{
    //enrollment gi vrzuva studentite so kursovite koi gi sledat
    public enum EnrollmentStatus { Enrolled, Completed, Dropped }; //ako e completed ke ima ocenka t.e ke ima grade
    public class Enrollment
    {
        [Key]
        public long Id { get; set; } //primary key
        
        [Required]
        public int CourseId { get; set; }

        [Required]
        public long StudentId { get; set; }

        [StringLength(10)]
        public string? Semester { get; set; } 

        public int? Year { get; set; }

        public int? Grade { get; set; }

        [StringLength(255)]
        public string? SeminarUrl { get; set; }
        //ovie gi dodavam za pod 4 baranjeto so files
        [StringLength(255)]
        public string? SeminarFileName { get; set; }

        public DateTime? SeminarUploadedAt { get; set; }

        [StringLength(255)]
        public string? ProjectUrl { get; set; }

        public int? ExamPoints { get; set; }

        public int? SeminarPoints { get; set; }
        public int? ProjectPoints { get; set; }

        public int? AdditionalPoints { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FinishDate { get; set; }

        //vo sustina site koi se so int i mozat da se nullable, ne mora da se definiraat so int?
        //no vo toj slucaj ako se samo so int, inicijalna vrednost ke bide 0 ako ne se dodeli druga
        // a za da moze da imaat i null vrednost togas treba so int?, zatoa taka gi definirav

        //bidejkji cources- students e many to many relacija, gi povrzuvame preku enrollment
        //imame 2 nadvoresni kluca kon entitetite student i course
        public Student Student { get; set; } = null!; //fk kon Student
        public Course Course { get; set; } = null!; //fk kon Course

        //za 3b baranjeto ke dodadam uste nekoi atributi za da moze da izgleda poubavo 
        public DateTime EnrolledOn { get; set; } = DateTime.Now; //DateTime.Now znaci deka koga ke se kreira nov objekt ke se zeme datumot i vremeto na kreiranje
        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Enrolled; //default ke e samo enrolled
        //ova ke mi dava info zapisan(ako uste go nema polozeno, a e uste na predmetot),polozen i otkazan (dokolku se ima otkazano od predemetot
        public bool IsRepeating { get; set; } //dali go ima prezapisano predmetot


    }
}
