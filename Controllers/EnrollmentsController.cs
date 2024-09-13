using AcademiaCoursePortal.API.Data;
using AcademiaCoursePortal.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AcademiaCoursePortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EnrollmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EnrollmentsController> _logger;

        public EnrollmentsController(ApplicationDbContext context, ILogger<EnrollmentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/enrollments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Enrollment>>> GetEnrollments()
        {
            try
            {
                var enrollments = await _context.Enrollments
                    .Include(e => e.Student)
                    .Include(e => e.Course)
                    .ToListAsync();
                return Ok(enrollments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving enrollments.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // GET: api/enrollments/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Enrollment>> GetEnrollment(int id)
        {
            try
            {
                var enrollment = await _context.Enrollments
                    .Include(e => e.Student)
                    .Include(e => e.Course)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (enrollment == null)
                {
                    return NotFound();
                }

                return Ok(enrollment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving the enrollment with ID {id}.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // POST: api/enrollments
        [HttpPost]
        public async Task<ActionResult<Enrollment>> PostEnrollment(Enrollment enrollment)
        {
            try
            {
                var studentExists = await _context.Students.AnyAsync(s => s.Id == enrollment.StudentId);
                var courseExists = await _context.Courses.AnyAsync(c => c.Id == enrollment.CourseId);

                if (!studentExists || !courseExists)
                {
                    return BadRequest("Student or Course not found.");
                }

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetEnrollment), new { id = enrollment.Id }, enrollment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the enrollment.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // PUT: api/enrollments/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEnrollment(int id, Enrollment enrollment)
        {
            if (id != enrollment.Id)
            {
                return BadRequest("Enrollment ID mismatch.");
            }

            _context.Entry(enrollment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!EnrollmentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, $"Concurrency error occurred while updating the enrollment with ID {id}.");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the enrollment.");
                return StatusCode(500, "Internal server error.");
            }

            return NoContent();
        }

        // DELETE: api/enrollments/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEnrollment(int id)
        {
            try
            {
                var enrollment = await _context.Enrollments.FindAsync(id);

                if (enrollment == null)
                {
                    return NotFound();
                }

                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while deleting the enrollment with ID {id}.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // GET: api/students/{studentId}/courses
        [HttpGet("students/{studentId}/courses")]
        public async Task<ActionResult<IEnumerable<Course>>> GetCoursesByStudent(int studentId)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Course)
                    .FirstOrDefaultAsync(s => s.Id == studentId);

                if (student == null)
                {
                    return NotFound();
                }

                var courses = student.Enrollments.Select(e => e.Course).ToList();
                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving courses for student with ID {studentId}.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // GET: api/enrollments/available-courses
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
                return StatusCode(500, "Internal server error.");
            }
        }

        private bool EnrollmentExists(int id)
        {
            return _context.Enrollments.Any(e => e.Id == id);
        }
    }
}
