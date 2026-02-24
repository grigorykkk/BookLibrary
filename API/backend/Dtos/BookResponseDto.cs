using System.Text.Json.Serialization;

namespace Backend.Dtos;

public sealed class BookResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<int> AuthorIds { get; set; } = [];
    public List<string> AuthorNames { get; set; } = [];
    public List<int> GenreIds { get; set; } = [];
    public List<string> GenreNames { get; set; } = [];
    public int PublishYear { get; set; }
    [JsonPropertyName("isbn")]
    public string ISBN { get; set; } = string.Empty;
    public int QuantityInStock { get; set; }
}
