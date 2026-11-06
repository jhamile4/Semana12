using System.ComponentModel.DataAnnotations;

namespace TallerMecanico.Models;

public class Vehiculo
{
    [Key]
    public int IdVehiculo { get; set; }
    [Required, StringLength(7, MinimumLength = 7)]
    [RegularExpression(@"^[A-Z0-9]{3}-[A-Z0-9]{3}$", ErrorMessage = "La placa debe tener formato ABC-123, en mayusculas.")]
    public string Placa { get; set; } = string.Empty;
    [Required, StringLength(30)]
    public string Marca { get; set; } = string.Empty;
    [Required, StringLength(50)]
    public string Modelo { get; set; } = string.Empty;
    [Required, Range(1900, 2100)]
    public int Anio { get; set; }
    [Required, Range(0, 9999999)]
    public int Kilometraje { get; set; }
    [Required, Range(1, int.MaxValue)]
    public int IdCliente { get; set; }
    public Cliente? Cliente { get; set; }
}
