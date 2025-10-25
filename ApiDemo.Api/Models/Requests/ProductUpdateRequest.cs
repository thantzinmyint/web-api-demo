using System.ComponentModel.DataAnnotations;

namespace ApiDemo.Api.Models.Requests;

public class ProductUpdateRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Price { get; set; }
}
