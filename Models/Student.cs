namespace AcademiaCoursePortal.API.Models
{
    public class Student
    {
        public int Id { get; set; }  

        public string Name { get; set; } = string.Empty;  

        public string Username { get; set; } = string.Empty;  

        public string? Email { get; set; }  

        public string? Password { get; set; }  

        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();  
    }
}
