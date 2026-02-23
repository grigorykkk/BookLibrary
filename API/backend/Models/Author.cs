namespace Backend.Models;

public sealed class Author
{
    public int Id { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public DateOnly BirthDate { get; set; }

    public string? Country { get; set; }

    public ICollection<Book> Books { get; set; } = new List<Book>();
}
