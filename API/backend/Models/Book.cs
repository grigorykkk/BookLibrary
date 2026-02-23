namespace Backend.Models;

public sealed class Book
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public int AuthorId { get; set; }

    public int GenreId { get; set; }

    public int PublishYear { get; set; }

    public required string ISBN { get; set; }

    public int QuantityInStock { get; set; }

    public Author? Author { get; set; }

    public Genre? Genre { get; set; }
}
