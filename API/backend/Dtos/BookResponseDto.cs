using System.Text.Json.Serialization;

namespace Backend.Dtos;

public sealed class BookResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public int GenreId { get; set; }
    public string GenreName { get; set; } = string.Empty;
    public int PublishYear { get; set; }
    [JsonPropertyName("isbn")]
    public string ISBN { get; set; } = string.Empty;
    public int QuantityInStock { get; set; }
}
