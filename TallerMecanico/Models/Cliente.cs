using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TallerMecanico.Models;

public class Cliente
{
    [Key]
    public int IdCliente { get; set; }
    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;
    [Required, StringLength(8, MinimumLength = 8)]
    [RegularExpression(@"^[0-9]{8}$", ErrorMessage = "El DNI debe contener ocho dígitos.")]
    public string Dni { get; set; } = string.Empty;
    [Required, StringLength(15)]
    public string Telefono { get; set; } = string.Empty;
    [Required, StringLength(100), EmailAddress]
    public string Email { get; set; } = string.Empty;
    [JsonIgnore]
    public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
}
