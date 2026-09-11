using FluentAssertions;
using Moq;
using Turnos.Application.DTOs;
using Turnos.Application.Services;
using Turnos.Domain.Entities;
using Turnos.Domain.Enums;
using Turnos.Domain.Exceptions;
using Turnos.Domain.Interfaces;
using Xunit;

namespace Turnos.Tests;

public class TurnoServiceTests
{
    private readonly Mock<ITurnoRepository> _turnoRepo = new();
    private readonly Mock<ISucursalRepository> _sucursalRepo = new();
    private readonly TurnoService _sut;

    public TurnoServiceTests()
    {
        _sut = new TurnoService(_turnoRepo.Object, _sucursalRepo.Object);
    }

    private static Sucursal SucursalActiva(int id = 1) => new() { Id = id, Nombre = "Centro", Direccion = "Cra 1", Ciudad = "Medellín", Activa = true };

    [Fact]
    public async Task CrearTurno_CuandoEsValido_DeberiaCrearloConEstadoPendiente()
    {
        _sucursalRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(SucursalActiva());
        _turnoRepo.Setup(r => r.CountByCedulaOnDateAsync("123456", It.IsAny<DateTime>(), default)).ReturnsAsync(0);

        var dto = new CrearTurnoDto("123456", 1);
        var resultado = await _sut.CrearTurnoAsync(dto);

        resultado.Estado.Should().Be(nameof(EstadoTurno.Pendiente));
        resultado.Cedula.Should().Be("123456");
        _turnoRepo.Verify(r => r.AddAsync(It.IsAny<Turno>(), default), Times.Once);
        _turnoRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CrearTurno_CuandoYaTieneCincoTurnosHoy_DeberiaLanzarExcepcion()
    {
        _sucursalRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(SucursalActiva());
        _turnoRepo.Setup(r => r.CountByCedulaOnDateAsync("123456", It.IsAny<DateTime>(), default)).ReturnsAsync(5);

        var dto = new CrearTurnoDto("123456", 1);
        var accion = () => _sut.CrearTurnoAsync(dto);

        await accion.Should().ThrowAsync<LimiteTurnosDiariosException>();
        _turnoRepo.Verify(r => r.AddAsync(It.IsAny<Turno>(), default), Times.Never);
    }

    [Fact]
    public async Task CrearTurno_CuandoSucursalNoExiste_DeberiaLanzarNotFound()
    {
        _sucursalRepo.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Sucursal?)null);

        var dto = new CrearTurnoDto("123456", 99);
        var accion = () => _sut.CrearTurnoAsync(dto);

        await accion.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CrearTurno_CuandoSucursalInactiva_DeberiaLanzarExcepcion()
    {
        var sucursalInactiva = SucursalActiva();
        sucursalInactiva.Activa = false;
        _sucursalRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(sucursalInactiva);

        var dto = new CrearTurnoDto("123456", 1);
        var accion = () => _sut.CrearTurnoAsync(dto);

        await accion.Should().ThrowAsync<SucursalInactivaException>();
    }

    [Fact]
    public async Task ActivarTurno_DentroDelTiempoLimite_DeberiaQuedarActivado()
    {
        var turno = Turno.Crear("123456", 1, "S01-240101-001", DateTime.UtcNow);
        _turnoRepo.Setup(r => r.GetByIdAsync(turno.Id, default)).ReturnsAsync(turno);

        var resultado = await _sut.ActivarTurnoAsync(turno.Id);

        resultado.Estado.Should().Be(nameof(EstadoTurno.Activado));
    }

    [Fact]
    public async Task ActivarTurno_DespuesDe15Minutos_DeberiaLanzarTurnoExpirado()
    {
        // Se crea el turno "hace 20 minutos" para simular que el límite ya venció.
        var turno = Turno.Crear("123456", 1, "S01-240101-001", DateTime.UtcNow.AddMinutes(-20));
        _turnoRepo.Setup(r => r.GetByIdAsync(turno.Id, default)).ReturnsAsync(turno);

        var accion = () => _sut.ActivarTurnoAsync(turno.Id);

        await accion.Should().ThrowAsync<TurnoExpiradoException>();
    }

    [Fact]
    public async Task ActualizarEstado_AAtendido_CuandoEstaActivado_DeberiaFuncionar()
    {
        var turno = Turno.Crear("123456", 1, "S01-240101-001", DateTime.UtcNow);
        turno.Activar(DateTime.UtcNow);
        _turnoRepo.Setup(r => r.GetByIdAsync(turno.Id, default)).ReturnsAsync(turno);

        var resultado = await _sut.ActualizarEstadoAsync(turno.Id, new ActualizarEstadoTurnoDto("Atendido"));

        resultado.Estado.Should().Be(nameof(EstadoTurno.Atendido));
    }

    [Fact]
    public async Task ActualizarEstado_AAtendido_CuandoAunEstaPendiente_DeberiaLanzarExcepcion()
    {
        var turno = Turno.Crear("123456", 1, "S01-240101-001", DateTime.UtcNow);
        _turnoRepo.Setup(r => r.GetByIdAsync(turno.Id, default)).ReturnsAsync(turno);

        var accion = () => _sut.ActualizarEstadoAsync(turno.Id, new ActualizarEstadoTurnoDto("Atendido"));

        await accion.Should().ThrowAsync<OperacionInvalidaException>();
    }
}
