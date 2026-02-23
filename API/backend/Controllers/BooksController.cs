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
public sealed class BooksController(LibraryDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookResponseDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? authorId,
        [FromQuery] int? genreId)
    {
        if (authorId is <= 0)
        {
            return BadRequest(new ApiErrorResponse("authorId must be greater than zero."));
        }

        if (genreId is <= 0)
        {
            return BadRequest(new ApiErrorResponse("genreId must be greater than zero."));
        }

        var query = dbContext.Books
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(book => EF.Functions.Like(book.Title, $"%{normalizedSearch}%"));
        }

        if (authorId.HasValue)
        {
            query = query.Where(book => book.AuthorId == authorId.Value);
        }

        if (genreId.HasValue)
        {
            query = query.Where(book => book.GenreId == genreId.Value);
        }

        var books = await query
            .OrderBy(book => book.Title)
            .Select(book => new BookResponseDto
            {
                Id = book.Id,
                Title = book.Title,
                AuthorId = book.AuthorId,
                AuthorName = $"{book.Author!.FirstName} {book.Author.LastName}".Trim(),
                GenreId = book.GenreId,
                GenreName = book.Genre!.Name,
                PublishYear = book.PublishYear,
                ISBN = book.ISBN,
                QuantityInStock = book.QuantityInStock
            })
            .ToListAsync();

        return Ok(books);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookResponseDto>> GetById(int id)
    {
        var book = await dbContext.Books
            .AsNoTracking()
            .Where(existingBook => existingBook.Id == id)
            .Select(existingBook => new BookResponseDto
            {
                Id = existingBook.Id,
                Title = existingBook.Title,
                AuthorId = existingBook.AuthorId,
                AuthorName = $"{existingBook.Author!.FirstName} {existingBook.Author.LastName}".Trim(),
                GenreId = existingBook.GenreId,
                GenreName = existingBook.Genre!.Name,
                PublishYear = existingBook.PublishYear,
                ISBN = existingBook.ISBN,
                QuantityInStock = existingBook.QuantityInStock
            })
            .FirstOrDefaultAsync();

        if (book is null)
        {
            return NotFound(new ApiErrorResponse($"Book with id {id} was not found."));
        }

        return Ok(book);
    }

    [HttpPost]
    public async Task<ActionResult<BookResponseDto>> Create([FromBody] BookRequestDto request)
    {
        if (!TryValidateRequest(request, out var errorMessage))
        {
            return BadRequest(new ApiErrorResponse(errorMessage));
        }

        var authorExists = await dbContext.Authors.AnyAsync(author => author.Id == request.AuthorId);
        if (!authorExists)
        {
            return BadRequest(new ApiErrorResponse($"Author with id {request.AuthorId} does not exist."));
        }

        var genreExists = await dbContext.Genres.AnyAsync(genre => genre.Id == request.GenreId);
        if (!genreExists)
        {
            return BadRequest(new ApiErrorResponse($"Genre with id {request.GenreId} does not exist."));
        }

        var book = new Book
        {
            Title = request.Title.Trim(),
            AuthorId = request.AuthorId,
            GenreId = request.GenreId,
            PublishYear = request.PublishYear,
            ISBN = request.ISBN.Trim(),
            QuantityInStock = request.QuantityInStock
        };

        dbContext.Books.Add(book);
        await dbContext.SaveChangesAsync();

        var response = await BuildResponseAsync(book.Id);
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BookResponseDto>> Update(int id, [FromBody] BookRequestDto request)
    {
        if (!TryValidateRequest(request, out var errorMessage))
        {
            return BadRequest(new ApiErrorResponse(errorMessage));
        }

        var book = await dbContext.Books.FirstOrDefaultAsync(existingBook => existingBook.Id == id);
        if (book is null)
        {
            return NotFound(new ApiErrorResponse($"Book with id {id} was not found."));
        }

        var authorExists = await dbContext.Authors.AnyAsync(author => author.Id == request.AuthorId);
        if (!authorExists)
        {
            return BadRequest(new ApiErrorResponse($"Author with id {request.AuthorId} does not exist."));
        }

        var genreExists = await dbContext.Genres.AnyAsync(genre => genre.Id == request.GenreId);
        if (!genreExists)
        {
            return BadRequest(new ApiErrorResponse($"Genre with id {request.GenreId} does not exist."));
        }

        book.Title = request.Title.Trim();
        book.AuthorId = request.AuthorId;
        book.GenreId = request.GenreId;
        book.PublishYear = request.PublishYear;
        book.ISBN = request.ISBN.Trim();
        book.QuantityInStock = request.QuantityInStock;

        await dbContext.SaveChangesAsync();

        var response = await BuildResponseAsync(book.Id);
        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await dbContext.Books.FirstOrDefaultAsync(existingBook => existingBook.Id == id);
        if (book is null)
        {
            return NotFound(new ApiErrorResponse($"Book with id {id} was not found."));
        }

        dbContext.Books.Remove(book);
        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    private async Task<BookResponseDto> BuildResponseAsync(int id)
    {
        return await dbContext.Books
            .AsNoTracking()
            .Where(book => book.Id == id)
            .Select(book => new BookResponseDto
            {
                Id = book.Id,
                Title = book.Title,
                AuthorId = book.AuthorId,
                AuthorName = $"{book.Author!.FirstName} {book.Author.LastName}".Trim(),
                GenreId = book.GenreId,
                GenreName = book.Genre!.Name,
                PublishYear = book.PublishYear,
                ISBN = book.ISBN,
                QuantityInStock = book.QuantityInStock
            })
            .SingleAsync();
    }

    private static bool TryValidateRequest(BookRequestDto request, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errorMessage = "Title is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.ISBN))
        {
            errorMessage = "ISBN is required.";
            return false;
        }

        if (request.QuantityInStock < 0)
        {
            errorMessage = "QuantityInStock cannot be negative.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
