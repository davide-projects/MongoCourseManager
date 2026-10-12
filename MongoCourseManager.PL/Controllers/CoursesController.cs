using Microsoft.AspNetCore.Mvc;
using MongoCourseManager.BLL.Models;
using MongoCourseManager.BLL.Services.Interfaces;

namespace MongoCourseManager.PL.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _service;

    public CoursesController(ICourseService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultModel<CourseModel>>> Search(
        [FromQuery] string? text,
        [FromQuery] string? category,
        [FromQuery] bool? isPublished,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool sortDescending = false)
    {
        var result = await _service.SearchAsync(
            text,
            category,
            isPublished,
            page,
            pageSize,
            sortDescending);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CourseModel>> GetById(string id)
    {
        var course = await _service.GetByIdAsync(id);

        return course is null
            ? NotFound()
            : Ok(course);
    }

    [HttpPost]
    public async Task<ActionResult<CourseModel>> Create(
        [FromBody] CreateCourseModel model)
    {
        try
        {
            var created = await _service.CreateAsync(model);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CourseModel>> Update(
        string id,
        [FromBody] UpdateCourseModel model)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, model);

            return updated is null
                ? NotFound()
                : Ok(updated);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _service.DeleteAsync(id);

        return deleted
            ? NoContent()
            : NotFound();
    }

    [HttpPatch("{id}/publication")]
    public async Task<IActionResult> SetPublished(
        string id,
        [FromQuery] bool published)
    {
        var updated = await _service.SetPublishedAsync(
            id,
            published);

        return updated
            ? NoContent()
            : NotFound();
    }

    [HttpPost("{id}/tags")]
    public async Task<IActionResult> AddTag(
        string id,
        [FromBody] string tag)
    {
        try
        {
            var updated = await _service.AddTagAsync(id, tag);

            return updated
                ? NoContent()
                : NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("{id}/tags/{tag}")]
    public async Task<IActionResult> RemoveTag(
        string id,
        string tag)
    {
        try
        {
            var updated = await _service.RemoveTagAsync(id, tag);

            return updated
                ? NoContent()
                : NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
