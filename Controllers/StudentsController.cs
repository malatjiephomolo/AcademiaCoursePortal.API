using BCrypt.Net;
using AcademiaCoursePortal.API.Data;
using AcademiaCoursePortal.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace AcademiaCoursePortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(ApplicationDbContext context, IConfiguration configuration, ILogger<StudentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/students
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Student>>> GetStudents()
        {
            try
            {
                var students = await _context.Students.Include(s => s.Enrollments)
                                                      .ThenInclude(e => e.Course)
                                                      .ToListAsync();
                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving students.");
                return StatusCode(500, "Internal server error occurred.");
            }
        }

        // GET: api/students/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Student>> GetStudent(int id)
        {
            try
            {
                var student = await _context.Students.Include(s => s.Enrollments)
                                                     .ThenInclude(e => e.Course)
                                                     .FirstOrDefaultAsync(s => s.Id == id);

                if (student == null)
                {
                    return NotFound("Student not found.");
                }

                return Ok(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving student with ID {id}.");
                return StatusCode(500, "Internal server error occurred.");
            }
        }

        // POST: api/students
        [HttpPost]
        public async Task<ActionResult<Student>> PostStudent([FromBody] Student student)
        {
            if (student == null)
            {
                return BadRequest("Student data is required.");
            }

            try
            {
                student.Password = BCrypt.Net.BCrypt.HashPassword(student.Password);

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, student);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "An error occurred while adding the student.");
                return StatusCode(500, "An error occurred while saving student data.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred.");
                return StatusCode(500, "Internal server error occurred.");
            }
        }

        // PUT: api/students/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] Student updatedStudent)
        {
            if (id != updatedStudent.Id || updatedStudent == null)
            {
                return BadRequest("Invalid student data.");
            }

            try
            {
                var existingStudent = await _context.Students.FindAsync(id);

                if (existingStudent == null)
                {
                    return NotFound("Student not found.");
                }

                // Update student properties
                existingStudent.Name = updatedStudent.Name;
                existingStudent.Email = updatedStudent.Email;

                // If password is provided and needs updating
                if (!string.IsNullOrEmpty(updatedStudent.Password))
                {
                    existingStudent.Password = BCrypt.Net.BCrypt.HashPassword(updatedStudent.Password);
                }

                _context.Entry(existingStudent).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "An error occurred while updating the student.");
                return StatusCode(500, "An error occurred while updating the student.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred.");
                return StatusCode(500, "Internal server error occurred.");
            }
        }

        // DELETE: api/students/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);

                if (student == null)
                {
                    return NotFound("Student not found.");
                }

                _context.Students.Remove(student);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while deleting student with ID {id}.");
                return StatusCode(500, "Internal server error occurred.");
            }
        }

        // POST: api/enrollments
        [HttpPost("enroll")]
        public async Task<ActionResult<Enrollment>> EnrollInCourse([FromBody] Enrollment enrollment)
        {
            if (enrollment == null)
            {
                return BadRequest("Enrollment data is required.");
            }

            try
            {
                var student = await _context.Students.FindAsync(enrollment.StudentId);
                var course = await _context.Courses.FindAsync(enrollment.CourseId);

                if (student == null || course == null)
                {
                    return BadRequest("Student or Course not found.");
                }

                if (await _context.Enrollments.AnyAsync(e => e.StudentId == enrollment.StudentId && e.CourseId == enrollment.CourseId))
                {
                    return BadRequest("Student is already enrolled in this course.");
                }

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetEnrolledCourses), new { id = enrollment.StudentId }, enrollment);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "An error occurred while enrolling in the course.");
                return StatusCode(500, "An error occurred while enrolling in the course.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred.");
                return StatusCode(500, "Internal server error occurred.");
            }
        }

        // DELETE: api/enrollments/{id}
        [HttpDelete("unenroll/{id}")]
        public async Task<IActionResult> UnenrollFromCourse(int id)
        {
            try
            {
                var enrollment = await _context.Enrollments.FindAsync(id);

                if (enrollment == null)
                {
                    return NotFound("Enrollment not found.");
                }

                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while unenrolling from the course with ID {id}.");
                return StatusCode(500, "Internal server error occurred.");
            }
        }

        // GET: api/students/{id}/courses
        [HttpGet("{id}/courses")]
        public async Task<ActionResult<IEnumerable<Course>>> GetEnrolledCourses(int id)
        {
            try
            {
                var student = await _context.Students.Include(s => s.Enrollments)
                                                     .ThenInclude(e => e.Course)
                                                     .FirstOrDefaultAsync(s => s.Id == id);

                if (student == null)
                {
                    return NotFound("Student not found.");
                }

                var courses = student.Enrollments.Select(e => e.Course).ToList();
                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving courses for student with ID {id}.");
                return StatusCode(500, "Internal server error occurred.");
            }
        }

        // GET: api/courses
        [HttpGet("available-courses")]
        public async Task<ActionResult<IEnumerable<Course>>> GetAvailableCourses()
        {
            try
            {
                var courses = await _context.Courses.ToListAsync();
                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving available courses.");
                return StatusCode(500, "Internal server error occurred.");
            }
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}
