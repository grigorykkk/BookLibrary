using System.Text.Json.Serialization;

namespace Backend.Dtos;

public sealed class BookResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<int> AuthorIds { get; set; } = [];
    public List<string> AuthorNames { get; set; } = [];
    public int GenreId { get; set; }
    public string GenreName { get; set; } = string.Empty;
    public int PublishYear { get; set; }
    [JsonPropertyName("isbn")]
    public string ISBN { get; set; } = string.Empty;
    public int QuantityInStock { get; set; }
}
