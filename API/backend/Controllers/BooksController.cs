using System.Text.RegularExpressions;
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
public sealed partial class BooksController(LibraryDbContext dbContext) : ControllerBase
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
            query = query.Where(book => book.BookAuthors.Any(ba => ba.AuthorId == authorId.Value));
        }

        if (genreId.HasValue)
        {
            query = query.Where(book => book.BookGenres.Any(bg => bg.GenreId == genreId.Value));
        }

        var books = await query
            .OrderBy(book => book.Title)
            .Select(book => new BookResponseDto
            {
                Id = book.Id,
                Title = book.Title,
                AuthorIds = book.BookAuthors.Select(ba => ba.AuthorId).ToList(),
                AuthorNames = book.BookAuthors
                    .Select(ba => (ba.Author!.FirstName + " " + ba.Author.LastName).Trim())
                    .ToList(),
                GenreIds = book.BookGenres.Select(bg => bg.GenreId).ToList(),
                GenreNames = book.BookGenres.Select(bg => bg.Genre!.Name).ToList(),
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
        var book = await BuildResponseQueryable()
            .Where(b => b.Id == id)
            .Select(b => MapToResponse(b))
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

        var trimmedIsbn = request.ISBN.Trim();
        var isbnDuplicate = await dbContext.Books.AnyAsync(b => b.ISBN == trimmedIsbn);
        if (isbnDuplicate)
        {
            return Conflict(new ApiErrorResponse($"A book with ISBN '{trimmedIsbn}' already exists."));
        }

        var distinctAuthorIds = request.AuthorIds.Distinct().ToList();
        var existingAuthorCount = await dbContext.Authors.CountAsync(a => distinctAuthorIds.Contains(a.Id));
        if (existingAuthorCount != distinctAuthorIds.Count)
        {
            return BadRequest(new ApiErrorResponse("One or more author IDs do not exist."));
        }

        var distinctGenreIds = request.GenreIds.Distinct().ToList();
        var existingGenreCount = await dbContext.Genres.CountAsync(g => distinctGenreIds.Contains(g.Id));
        if (existingGenreCount != distinctGenreIds.Count)
        {
            return BadRequest(new ApiErrorResponse("One or more genre IDs do not exist."));
        }

        var book = new Book
        {
            Title = request.Title.Trim(),
            PublishYear = request.PublishYear,
            ISBN = trimmedIsbn,
            QuantityInStock = request.QuantityInStock
        };

        dbContext.Books.Add(book);
        await dbContext.SaveChangesAsync();

        foreach (var aid in distinctAuthorIds)
        {
            dbContext.BookAuthors.Add(new BookAuthor { BookId = book.Id, AuthorId = aid });
        }

        foreach (var gid in distinctGenreIds)
        {
            dbContext.BookGenres.Add(new BookGenre { BookId = book.Id, GenreId = gid });
        }

        await dbContext.SaveChangesAsync();

        var response = await BuildSingleResponseAsync(book.Id);
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BookResponseDto>> Update(int id, [FromBody] BookRequestDto request)
    {
        if (!TryValidateRequest(request, out var errorMessage))
        {
            return BadRequest(new ApiErrorResponse(errorMessage));
        }

        var book = await dbContext.Books
            .Include(b => b.BookAuthors)
            .Include(b => b.BookGenres)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (book is null)
        {
            return NotFound(new ApiErrorResponse($"Book with id {id} was not found."));
        }

        var trimmedIsbn = request.ISBN.Trim();
        var isbnDuplicate = await dbContext.Books.AnyAsync(b => b.ISBN == trimmedIsbn && b.Id != id);
        if (isbnDuplicate)
        {
            return Conflict(new ApiErrorResponse($"A book with ISBN '{trimmedIsbn}' already exists."));
        }

        var distinctAuthorIds = request.AuthorIds.Distinct().ToList();
        var existingAuthorCount = await dbContext.Authors.CountAsync(a => distinctAuthorIds.Contains(a.Id));
        if (existingAuthorCount != distinctAuthorIds.Count)
        {
            return BadRequest(new ApiErrorResponse("One or more author IDs do not exist."));
        }

        var distinctGenreIds = request.GenreIds.Distinct().ToList();
        var existingGenreCount = await dbContext.Genres.CountAsync(g => distinctGenreIds.Contains(g.Id));
        if (existingGenreCount != distinctGenreIds.Count)
        {
            return BadRequest(new ApiErrorResponse("One or more genre IDs do not exist."));
        }

        book.Title = request.Title.Trim();
        book.PublishYear = request.PublishYear;
        book.ISBN = trimmedIsbn;
        book.QuantityInStock = request.QuantityInStock;

        book.BookAuthors.Clear();
        foreach (var aid in distinctAuthorIds)
        {
            book.BookAuthors.Add(new BookAuthor { BookId = book.Id, AuthorId = aid });
        }

        book.BookGenres.Clear();
        foreach (var gid in distinctGenreIds)
        {
            book.BookGenres.Add(new BookGenre { BookId = book.Id, GenreId = gid });
        }

        await dbContext.SaveChangesAsync();

        var response = await BuildSingleResponseAsync(book.Id);
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

    private IQueryable<Book> BuildResponseQueryable()
    {
        return dbContext.Books
            .AsNoTracking()
            .Include(b => b.BookAuthors)
            .ThenInclude(ba => ba.Author)
            .Include(b => b.BookGenres)
            .ThenInclude(bg => bg.Genre);
    }

    private async Task<BookResponseDto> BuildSingleResponseAsync(int id)
    {
        return await BuildResponseQueryable()
            .Where(b => b.Id == id)
            .Select(b => new BookResponseDto
            {
                Id = b.Id,
                Title = b.Title,
                AuthorIds = b.BookAuthors.Select(ba => ba.AuthorId).ToList(),
                AuthorNames = b.BookAuthors
                    .Select(ba => (ba.Author!.FirstName + " " + ba.Author.LastName).Trim())
                    .ToList(),
                GenreIds = b.BookGenres.Select(bg => bg.GenreId).ToList(),
                GenreNames = b.BookGenres.Select(bg => bg.Genre!.Name).ToList(),
                PublishYear = b.PublishYear,
                ISBN = b.ISBN,
                QuantityInStock = b.QuantityInStock
            })
            .SingleAsync();
    }

    private static BookResponseDto MapToResponse(Book b)
    {
        return new BookResponseDto
        {
            Id = b.Id,
            Title = b.Title,
            AuthorIds = b.BookAuthors.Select(ba => ba.AuthorId).ToList(),
            AuthorNames = b.BookAuthors
                .Select(ba => (ba.Author!.FirstName + " " + ba.Author.LastName).Trim())
                .ToList(),
            GenreIds = b.BookGenres.Select(bg => bg.GenreId).ToList(),
            GenreNames = b.BookGenres.Select(bg => bg.Genre!.Name).ToList(),
            PublishYear = b.PublishYear,
            ISBN = b.ISBN,
            QuantityInStock = b.QuantityInStock
        };
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

        if (!IsbnRegex().IsMatch(request.ISBN.Trim()))
        {
            errorMessage = "ISBN must contain only digits (1-13).";
            return false;
        }

        if (request.AuthorIds is null || request.AuthorIds.Count == 0)
        {
            errorMessage = "At least one author is required.";
            return false;
        }

        if (request.AuthorIds.Any(id => id <= 0))
        {
            errorMessage = "All author IDs must be greater than zero.";
            return false;
        }

        if (request.GenreIds is null || request.GenreIds.Count == 0)
        {
            errorMessage = "At least one genre is required.";
            return false;
        }

        if (request.GenreIds.Any(id => id <= 0))
        {
            errorMessage = "All genre IDs must be greater than zero.";
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

    [GeneratedRegex(@"^\d{1,13}$")]
    private static partial Regex IsbnRegex();
}
