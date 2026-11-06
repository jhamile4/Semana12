using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TallerMecanico.Data;
using TallerMecanico.Models;

namespace TallerMecanico.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController(TallerContext context) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Cliente>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Cliente>>> GetAll([FromQuery] string? buscar)
    {
        var query = context.Clientes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim().ToLower();
            query = query.Where(p => p.Nombre.ToLower().Contains(texto) ||
                p.Dni.Contains(texto) || p.Email.ToLower().Contains(texto));
        }
        return Ok(await query.OrderBy(p => p.IdCliente).ToListAsync());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Cliente), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Cliente>> GetById(int id)
    {
        var cliente = await context.Clientes.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdCliente == id);
        return cliente is null ? NotFound() : Ok(cliente);
    }

    [HttpGet("{id:int}/vehiculos")]
    [ProducesResponseType(typeof(IEnumerable<Vehiculo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<Vehiculo>>> GetVehiculos(int id)
    {
        if (!await context.Clientes.AnyAsync(p => p.IdCliente == id)) return NotFound();
        return Ok(await context.Vehiculos.AsNoTracking().Where(m => m.IdCliente == id)
            .OrderBy(m => m.IdVehiculo).ToListAsync());
    }

    [HttpPost]
    [ProducesResponseType(typeof(Cliente), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Cliente>> Create(Cliente cliente)
    {
        if (cliente.IdCliente != 0) return BadRequest("El ID se genera automáticamente; envíe 0 u omítalo.");
        Normalizar(cliente);
        if (await Duplicado(cliente)) return Conflict("Ya existe un cliente con ese DNI o email.");
        cliente.Vehiculos = new List<Vehiculo>();
        context.Clientes.Add(cliente);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Conflict("Ya existe un cliente con ese DNI o email."); }
        return CreatedAtAction(nameof(GetById), new { id = cliente.IdCliente }, cliente);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, Cliente cliente)
    {
        if (id != cliente.IdCliente) return BadRequest("El ID de la ruta y del cuerpo deben coincidir.");
        var actual = await context.Clientes.FindAsync(id);
        if (actual is null) return NotFound();
        Normalizar(cliente);
        if (await Duplicado(cliente)) return Conflict("Ya existe un cliente con ese DNI o email.");
        actual.Nombre = cliente.Nombre;
        actual.Dni = cliente.Dni;
        actual.Telefono = cliente.Telefono;
        actual.Email = cliente.Email;
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Conflict("Ya existe un cliente con ese DNI o email."); }
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var cliente = await context.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();
        if (await context.Vehiculos.AnyAsync(m => m.IdCliente == id))
            return Conflict("No se puede eliminar un cliente con vehiculos vinculadas.");
        context.Clientes.Remove(cliente);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return NotFound(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        { return Conflict("No se puede eliminar un cliente con vehiculos vinculadas."); }
        return NoContent();
    }

    private Task<bool> Duplicado(Cliente p) => context.Clientes.AnyAsync(x =>
        x.IdCliente != p.IdCliente && (x.Dni == p.Dni || x.Email.ToLower() == p.Email));

    private static void Normalizar(Cliente p)
    {
        p.Nombre = p.Nombre.Trim();
        p.Dni = p.Dni.Trim();
        p.Telefono = p.Telefono.Trim();
        p.Email = p.Email.Trim().ToLowerInvariant();
    }
}
