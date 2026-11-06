using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TallerMecanico.Data;
using TallerMecanico.Models;

namespace TallerMecanico.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiculosController(TallerContext context) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Vehiculo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Vehiculo>>> GetAll([FromQuery] string? marca)
    {
        var query = context.Vehiculos.AsNoTracking().Include(m => m.Cliente).AsQueryable();
        if (!string.IsNullOrWhiteSpace(marca))
        {
            var texto = marca.Trim().ToLower();
            query = query.Where(m => m.Marca.ToLower().Contains(texto));
        }
        return Ok(await query.OrderBy(m => m.IdVehiculo).ToListAsync());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Vehiculo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Vehiculo>> GetById(int id)
    {
        var vehiculo = await context.Vehiculos.AsNoTracking().Include(m => m.Cliente)
            .FirstOrDefaultAsync(m => m.IdVehiculo == id);
        return vehiculo is null ? NotFound() : Ok(vehiculo);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Vehiculo), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Vehiculo>> Create(Vehiculo vehiculo)
    {
        if (vehiculo.IdVehiculo != 0) return BadRequest("El ID se genera automáticamente; envíe 0 u omítalo.");
        vehiculo.Placa = vehiculo.Placa.Trim().ToUpperInvariant();
        if (!await context.Clientes.AnyAsync(p => p.IdCliente == vehiculo.IdCliente))
            return BadRequest("El cliente indicado no existe.");
        if (await context.Vehiculos.AnyAsync(v => v.Placa == vehiculo.Placa))
            return Conflict("Ya existe un vehiculo con esa placa.");
        vehiculo.Cliente = null;
        context.Vehiculos.Add(vehiculo);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        { return BadRequest("El cliente indicado no existe."); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Conflict("Ya existe un vehiculo con esa placa."); }
        return CreatedAtAction(nameof(GetById), new { id = vehiculo.IdVehiculo }, vehiculo);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, Vehiculo vehiculo)
    {
        if (id != vehiculo.IdVehiculo) return BadRequest("El ID de la ruta y del cuerpo deben coincidir.");
        var actual = await context.Vehiculos.FindAsync(id);
        if (actual is null) return NotFound();
        vehiculo.Placa = vehiculo.Placa.Trim().ToUpperInvariant();
        if (await context.Vehiculos.AnyAsync(v => v.IdVehiculo != id && v.Placa == vehiculo.Placa))
            return Conflict("Ya existe un vehiculo con esa placa.");
        if (!await context.Clientes.AnyAsync(p => p.IdCliente == vehiculo.IdCliente))
            return BadRequest("El cliente indicado no existe.");
        actual.Placa = vehiculo.Placa;
        actual.Marca = vehiculo.Marca;
        actual.Modelo = vehiculo.Modelo;
        actual.Anio = vehiculo.Anio;
        actual.Kilometraje = vehiculo.Kilometraje;
        actual.IdCliente = vehiculo.IdCliente;
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        { return BadRequest("El cliente indicado no existe."); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Conflict("Ya existe un vehiculo con esa placa."); }
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var vehiculo = await context.Vehiculos.FindAsync(id);
        if (vehiculo is null) return NotFound();
        context.Vehiculos.Remove(vehiculo);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        return NoContent();
    }
}
