using System.ComponentModel.DataAnnotations;

namespace Backend.Dtos;

public sealed class BookRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<int> AuthorIds { get; set; } = [];

    [Range(1, int.MaxValue)]
    public int GenreId { get; set; }

    [Range(1, 9999)]
    public int PublishYear { get; set; }

    [Required]
    [MaxLength(13)]
    [RegularExpression(@"^\d{1,13}$", ErrorMessage = "ISBN must contain only digits (1-13).")]
    public string ISBN { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int QuantityInStock { get; set; }
}
