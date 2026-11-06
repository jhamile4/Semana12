using Microsoft.EntityFrameworkCore;
using TallerMecanico.Models;

namespace TallerMecanico.Data;

public class TallerContext(DbContextOptions<TallerContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Clientes");
            entity.HasIndex(p => p.Dni).IsUnique().HasDatabaseName("UQ_Clientes_Dni");
            entity.HasIndex(p => p.Email).IsUnique().HasDatabaseName("UQ_Clientes_Email");
        });
        modelBuilder.Entity<Vehiculo>(entity =>
        {
            entity.ToTable("Vehiculos");
            entity.HasIndex(v => v.Placa).IsUnique().HasDatabaseName("UQ_Vehiculos_Placa");
            entity.HasOne(m => m.Cliente).WithMany(p => p.Vehiculos)
                .HasForeignKey(m => m.IdCliente).OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Vehiculos_Clientes");
        });
    }
}
