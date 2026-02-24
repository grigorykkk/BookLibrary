namespace Backend.Models;

public sealed class Book
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public int GenreId { get; set; }

    public int PublishYear { get; set; }

    public required string ISBN { get; set; }

    public int QuantityInStock { get; set; }

    public Genre? Genre { get; set; }

    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
}
