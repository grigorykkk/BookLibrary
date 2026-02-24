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
public sealed class GenresController(LibraryDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GenreResponseDto>>> GetAll()
    {
        var genres = await dbContext.Genres
            .AsNoTracking()
            .OrderBy(genre => genre.Name)
            .Select(genre => new GenreResponseDto
            {
                Id = genre.Id,
                Name = genre.Name,
                Description = genre.Description
            })
            .ToListAsync();

        return Ok(genres);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GenreResponseDto>> GetById(int id)
    {
        var genre = await dbContext.Genres
            .AsNoTracking()
            .Where(existingGenre => existingGenre.Id == id)
            .Select(existingGenre => new GenreResponseDto
            {
                Id = existingGenre.Id,
                Name = existingGenre.Name,
                Description = existingGenre.Description
            })
            .FirstOrDefaultAsync();

        if (genre is null)
        {
            return NotFound(new ApiErrorResponse($"Genre with id {id} was not found."));
        }

        return Ok(genre);
    }

    [HttpPost]
    public async Task<ActionResult<GenreResponseDto>> Create([FromBody] GenreRequestDto request)
    {
        if (!TryValidateRequest(request, out var errorMessage))
        {
            return BadRequest(new ApiErrorResponse(errorMessage));
        }

        var normalizedName = request.Name.Trim();

        var existingGenres = await dbContext.Genres
            .Select(g => g.Name)
            .ToListAsync();

        var duplicate = existingGenres.Any(name =>
            string.Equals(name, normalizedName, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            return Conflict(new ApiErrorResponse($"Genre '{normalizedName}' already exists."));
        }

        var genre = new Genre
        {
            Name = normalizedName,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };

        dbContext.Genres.Add(genre);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = genre.Id }, ToResponse(genre));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<GenreResponseDto>> Update(int id, [FromBody] GenreRequestDto request)
    {
        if (!TryValidateRequest(request, out var errorMessage))
        {
            return BadRequest(new ApiErrorResponse(errorMessage));
        }

        var genre = await dbContext.Genres.FirstOrDefaultAsync(existingGenre => existingGenre.Id == id);
        if (genre is null)
        {
            return NotFound(new ApiErrorResponse($"Genre with id {id} was not found."));
        }

        var normalizedName = request.Name.Trim();

        var existingGenres = await dbContext.Genres
            .Where(g => g.Id != id)
            .Select(g => g.Name)
            .ToListAsync();

        var duplicate = existingGenres.Any(name =>
            string.Equals(name, normalizedName, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            return Conflict(new ApiErrorResponse($"Genre '{normalizedName}' already exists."));
        }

        genre.Name = normalizedName;
        genre.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        await dbContext.SaveChangesAsync();

        return Ok(ToResponse(genre));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var genre = await dbContext.Genres.FirstOrDefaultAsync(existingGenre => existingGenre.Id == id);
        if (genre is null)
        {
            return NotFound(new ApiErrorResponse($"Genre with id {id} was not found."));
        }

        dbContext.Genres.Remove(genre);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    private static bool TryValidateRequest(GenreRequestDto request, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errorMessage = "Name is required.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static GenreResponseDto ToResponse(Genre genre)
    {
        return new GenreResponseDto
        {
            Id = genre.Id,
            Name = genre.Name,
            Description = genre.Description
        };
    }
}
