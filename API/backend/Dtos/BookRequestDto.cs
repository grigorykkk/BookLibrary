using System.ComponentModel.DataAnnotations;

namespace Backend.Dtos;

public sealed class BookRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int AuthorId { get; set; }

    [Range(1, int.MaxValue)]
    public int GenreId { get; set; }

    [Range(1, 9999)]
    public int PublishYear { get; set; }

    [Required]
    [MaxLength(32)]
    public string ISBN { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int QuantityInStock { get; set; }
}
