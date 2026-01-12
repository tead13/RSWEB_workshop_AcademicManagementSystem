using Microsoft.AspNetCore.Identity;

namespace AcademicManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        //za profesor i student 
        public long? StudentId { get; set; }
        public int? TeacherId { get; set; }
    }
}
