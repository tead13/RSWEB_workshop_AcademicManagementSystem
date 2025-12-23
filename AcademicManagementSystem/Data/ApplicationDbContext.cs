using Microsoft.EntityFrameworkCore;
using AcademicManagementSystem.Models;

namespace AcademicManagementSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        //ApplicationDbContext EF core, gi povrzuva modelite i bazata 
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        } //konstruktor na klasata
        //DbSet - tabela vo bazata, zavtomatski kreira tabela vrz osnova na modelot
        public DbSet<Student> Students { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Student - Enrollment (one-to-many)
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentId);

            // Course - Enrollment (one-to-many)
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId);

            // Course → FirstTeacher (one-to-many)
            modelBuilder.Entity<Course>()
                .HasOne(c => c.FirstTeacher)
                .WithMany(t => t.FirstTeacherCourses)
                .HasForeignKey(c => c.FirstTeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // Course → SecondTeacher (one-to-many)
            modelBuilder.Entity<Course>()
                .HasOne(c => c.SecondTeacher)
                .WithMany(t => t.SecondTeacherCourses)
                .HasForeignKey(c => c.SecondTeacherId)
                .OnDelete(DeleteBehavior.Restrict); //restrict delete to avoid cascading deletions
            //ako se izbrise nastavnik da ne se izbrise predmetot
        }

    }
}



