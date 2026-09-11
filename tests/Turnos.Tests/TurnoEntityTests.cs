using FluentAssertions;
using Turnos.Domain.Entities;
using Turnos.Domain.Enums;
using Turnos.Domain.Exceptions;
using Xunit;

namespace Turnos.Tests;

public class TurnoEntityTests
{
    [Fact]
    public void Crear_DeberiaEstablecerExpiracionA15Minutos()
    {
        var ahora = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var turno = Turno.Crear("123456", 1, "COD-1", ahora);

        turno.FechaHoraExpiracion.Should().Be(ahora.AddMinutes(15));
        turno.Estado.Should().Be(EstadoTurno.Pendiente);
    }

    [Fact]
    public void IntentarExpirar_SiVencioYSigueEnPendiente_DeberiaMarcarExpirado()
    {
        var turno = Turno.Crear("123456", 1, "COD-1", DateTime.UtcNow.AddMinutes(-16));

        var expiro = turno.IntentarExpirar(DateTime.UtcNow);

        expiro.Should().BeTrue();
        turno.Estado.Should().Be(EstadoTurno.Expirado);
    }

    [Fact]
    public void Cancelar_CuandoYaFueAtendido_DeberiaLanzarExcepcion()
    {
        var turno = Turno.Crear("123456", 1, "COD-1", DateTime.UtcNow);
        turno.Activar(DateTime.UtcNow);
        turno.MarcarAtendido();

        var accion = () => turno.Cancelar();

        accion.Should().Throw<OperacionInvalidaException>();
    }
}
