using AcademicManagementSystem.ViewModels;

namespace AcademicManagementSystem.ViewModels
{
    public class EditCourseVM
    {
        //site sto sakame da gi editneme
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Credits { get; set; }
        public int Semester { get; set; }
        public string Programme { get; set; } = string.Empty;
        public string EducationLevel { get; set; } = string.Empty;

        public int? FirstTeacherId { get; set; }
        public int? SecondTeacherId { get; set; }

        // lista od site studenti + enrollment info
        /*ova e od faza1 nacin na nrollment vo faza2 ke bide na dr nacin
         public List<CourseEnrollmentVM> Students { get; set; } = new List<CourseEnrollmentVM>();
        */
    }
}
