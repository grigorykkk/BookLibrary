namespace Backend.Models;

public sealed class Genre
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public ICollection<Book> Books { get; set; } = new List<Book>();
}
