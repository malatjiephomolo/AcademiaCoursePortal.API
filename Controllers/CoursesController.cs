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
    public class CoursesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CoursesController> _logger;

        public CoursesController(ApplicationDbContext context, ILogger<CoursesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/courses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Course>>> GetCourses()
        {
            try
            {
                var courses = await _context.Courses.Include(c => c.Enrollments).ToListAsync();
                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving courses.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // GET: api/courses/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Course>> GetCourse(int id)
        {
            try
            {
                var course = await _context.Courses.Include(c => c.Enrollments)
                                                   .FirstOrDefaultAsync(c => c.Id == id);

                if (course == null)
                {
                    return NotFound();
                }

                return Ok(course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving the course with ID {id}.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // POST: api/courses
        [HttpPost]
        public async Task<ActionResult<Course>> PostCourse(Course course)
        {
            try
            {
                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the course.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // PUT: api/courses/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCourse(int id, Course course)
        {
            if (id != course.Id)
            {
                return BadRequest("Course ID mismatch.");
            }

            _context.Entry(course).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!CourseExists(id))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, $"Concurrency error occurred while updating the course with ID {id}.");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the course.");
                return StatusCode(500, "Internal server error.");
            }

            return NoContent();
        }

        // DELETE: api/courses/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);

                if (course == null)
                {
                    return NotFound();
                }

                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while deleting the course with ID {id}.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // GET: api/courses/{courseId}/students
        [HttpGet("{courseId}/students")]
        public async Task<ActionResult<IEnumerable<Student>>> GetStudentsByCourse(int courseId)
        {
            try
            {
                var course = await _context.Courses.Include(c => c.Enrollments)
                                                   .ThenInclude(e => e.Student)
                                                   .FirstOrDefaultAsync(c => c.Id == courseId);

                if (course == null)
                {
                    return NotFound();
                }

                var students = course.Enrollments.Select(e => e.Student).ToList();
                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving students for course with ID {courseId}.");
                return StatusCode(500, "Internal server error.");
            }
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }
    }
}
