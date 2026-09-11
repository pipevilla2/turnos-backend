namespace Turnos.Domain.Entities;

public class Sucursal
{
    public int Id { get; set; }
    public string Nombre { get; set; } = default!;
    public string Direccion { get; set; } = default!;
    public string Ciudad { get; set; } = default!;
    public bool Activa { get; set; } = true;

    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
}
