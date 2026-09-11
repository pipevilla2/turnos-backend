using Turnos.Domain.Entities;

namespace Turnos.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string entidad, object id) : base($"{entidad} con id '{id}' no fue encontrado.") { }
}

public class SucursalInactivaException : DomainException
{
    public SucursalInactivaException(int sucursalId) : base($"La sucursal {sucursalId} no está activa para agendamiento.") { }
}

public class LimiteTurnosDiariosException : DomainException
{
    public LimiteTurnosDiariosException(string cedula)
        : base($"La cédula {cedula} ya alcanzó el máximo de {Turno.MaxTurnosDiarios} turnos solicitados hoy. Podrá generar uno nuevo a partir del día siguiente.") { }
}

public class TurnoExpiradoException : DomainException
{
    public TurnoExpiradoException(Guid turnoId) : base($"El turno {turnoId} expiró porque no fue activado dentro de los {Turno.MinutosLimiteActivacion} minutos límite.") { }
}

public class OperacionInvalidaException : DomainException
{
    public OperacionInvalidaException(string message) : base(message) { }
}
