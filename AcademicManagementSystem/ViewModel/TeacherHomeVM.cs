using System.Collections.Generic;

namespace AcademicManagementSystem.ViewModels
{
    public class TeacherHomeVM
    {
        public string TeacherFullName { get; set; } = "";
        public List<TeacherCourseListItemVM> Courses { get; set; } = new();
    }
}
