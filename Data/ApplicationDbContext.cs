using Microsoft.EntityFrameworkCore;
using AcademiaCoursePortal.API.Models;

namespace AcademiaCoursePortal.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure relationships
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentId);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId);

            // Ensure the Username is unique
            modelBuilder.Entity<Student>()
                .HasIndex(s => s.Username)
                .IsUnique();

            
            modelBuilder.Entity<Student>()
                .Property(s => s.Id)
                .ValueGeneratedOnAdd();  

            base.OnModelCreating(modelBuilder);
        }
    }
}
