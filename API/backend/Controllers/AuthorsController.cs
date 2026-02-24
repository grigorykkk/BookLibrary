using Backend.Common;
using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AuthorsController(LibraryDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuthorResponseDto>>> GetAll()
    {
        var authors = await dbContext.Authors
            .AsNoTracking()
            .OrderBy(author => author.LastName)
            .ThenBy(author => author.FirstName)
            .Select(author => new AuthorResponseDto
            {
                Id = author.Id,
                FirstName = author.FirstName,
                LastName = author.LastName,
                BirthDate = author.BirthDate,
                Country = author.Country
            })
            .ToListAsync();

        return Ok(authors);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AuthorResponseDto>> GetById(int id)
    {
        var author = await dbContext.Authors
            .AsNoTracking()
            .Where(existingAuthor => existingAuthor.Id == id)
            .Select(existingAuthor => new AuthorResponseDto
            {
                Id = existingAuthor.Id,
                FirstName = existingAuthor.FirstName,
                LastName = existingAuthor.LastName,
                BirthDate = existingAuthor.BirthDate,
                Country = existingAuthor.Country
            })
            .FirstOrDefaultAsync();

        if (author is null)
        {
            return NotFound(new ApiErrorResponse($"Author with id {id} was not found."));
        }

        return Ok(author);
    }

    [HttpPost]
    public async Task<ActionResult<AuthorResponseDto>> Create([FromBody] AuthorRequestDto request)
    {
        if (!TryValidateRequest(request, out var errorMessage))
        {
            return BadRequest(new ApiErrorResponse(errorMessage));
        }

        var normalizedFirst = request.FirstName.Trim();
        var normalizedLast = request.LastName.Trim();

        var existingAuthors = await dbContext.Authors
            .Select(a => new { a.FirstName, a.LastName })
            .ToListAsync();

        var duplicate = existingAuthors.Any(a =>
            string.Equals(a.FirstName, normalizedFirst, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.LastName, normalizedLast, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            return Conflict(new ApiErrorResponse(
                $"Author '{normalizedFirst} {normalizedLast}' already exists."));
        }

        var author = new Author
        {
            FirstName = normalizedFirst,
            LastName = normalizedLast,
            BirthDate = request.BirthDate,
            Country = string.IsNullOrWhiteSpace(request.Country) ? null : request.Country.Trim()
        };

        dbContext.Authors.Add(author);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = author.Id },
            ToResponse(author));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AuthorResponseDto>> Update(int id, [FromBody] AuthorRequestDto request)
    {
        if (!TryValidateRequest(request, out var errorMessage))
        {
            return BadRequest(new ApiErrorResponse(errorMessage));
        }

        var author = await dbContext.Authors.FirstOrDefaultAsync(existingAuthor => existingAuthor.Id == id);
        if (author is null)
        {
            return NotFound(new ApiErrorResponse($"Author with id {id} was not found."));
        }

        var normalizedFirst = request.FirstName.Trim();
        var normalizedLast = request.LastName.Trim();

        var existingAuthors = await dbContext.Authors
            .Where(a => a.Id != id)
            .Select(a => new { a.FirstName, a.LastName })
            .ToListAsync();

        var duplicate = existingAuthors.Any(a =>
            string.Equals(a.FirstName, normalizedFirst, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.LastName, normalizedLast, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            return Conflict(new ApiErrorResponse(
                $"Author '{normalizedFirst} {normalizedLast}' already exists."));
        }

        author.FirstName = normalizedFirst;
        author.LastName = normalizedLast;
        author.BirthDate = request.BirthDate;
        author.Country = string.IsNullOrWhiteSpace(request.Country) ? null : request.Country.Trim();

        await dbContext.SaveChangesAsync();

        return Ok(ToResponse(author));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var author = await dbContext.Authors.FirstOrDefaultAsync(existingAuthor => existingAuthor.Id == id);
        if (author is null)
        {
            return NotFound(new ApiErrorResponse($"Author with id {id} was not found."));
        }

        dbContext.Authors.Remove(author);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    private static bool TryValidateRequest(AuthorRequestDto request, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            errorMessage = "FirstName is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            errorMessage = "LastName is required.";
            return false;
        }

        if (request.BirthDate == default)
        {
            errorMessage = "BirthDate is required.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static AuthorResponseDto ToResponse(Author author)
    {
        return new AuthorResponseDto
        {
            Id = author.Id,
            FirstName = author.FirstName,
            LastName = author.LastName,
            BirthDate = author.BirthDate,
            Country = author.Country
        };
    }
}
