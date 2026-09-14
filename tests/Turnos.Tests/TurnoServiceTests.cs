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
    public async Task CrearTurno_CuandoEsValido_DeberiaCrearloConEstadoPendienteYCodigoDeConsecutivo()
    {
        var fechaHoy = DateTime.UtcNow;
        _sucursalRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(SucursalActiva());
        _turnoRepo.Setup(r => r.CountByCedulaBetweenAsync("123456", It.IsAny<DateTime>(), It.IsAny<DateTime>(), default)).ReturnsAsync(0);
        _turnoRepo.Setup(r => r.GetNextConsecutivoAsync(1, default)).ReturnsAsync(1);

        var dto = new CrearTurnoDto("123456", 1);
        var resultado = await _sut.CrearTurnoAsync(dto);

        resultado.Estado.Should().Be(nameof(EstadoTurno.Pendiente));
        resultado.Cedula.Should().Be("123456");
        resultado.CodigoTurno.Should().Be("S01-001");
        _turnoRepo.Verify(r => r.GetNextConsecutivoAsync(1, default), Times.Once);
        _turnoRepo.Verify(r => r.AddAsync(It.IsAny<Turno>(), default), Times.Once);
        _turnoRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CrearTurno_CuandoHayConsecutivoDeSucursal_DeberiaGenerarElSiguienteCodigo()
    {
        var fechaHoy = DateTime.UtcNow;
        _sucursalRepo.Setup(r => r.GetByIdAsync(3, default)).ReturnsAsync(SucursalActiva(3));
        _turnoRepo.Setup(r => r.CountByCedulaBetweenAsync("999999", It.IsAny<DateTime>(), It.IsAny<DateTime>(), default)).ReturnsAsync(0);
        _turnoRepo.Setup(r => r.GetNextConsecutivoAsync(3, default)).ReturnsAsync(12);

        var dto = new CrearTurnoDto("999999", 3);
        var resultado = await _sut.CrearTurnoAsync(dto);

        resultado.CodigoTurno.Should().Be("S03-012");
    }

    [Fact]
    public async Task CrearTurno_CuandoYaTieneCincoTurnosHoy_DeberiaLanzarExcepcion()
    {
        _sucursalRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(SucursalActiva());
        _turnoRepo.Setup(r => r.CountByCedulaBetweenAsync("123456", It.IsAny<DateTime>(), It.IsAny<DateTime>(), default)).ReturnsAsync(5);

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
    public void ActivarTurno_DespuesDe15Minutos_DeberiaLanzarTurnoExpirado()
    {
        var ahora = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var turno = Turno.Crear("123456", 1, "S01-240101-001", ahora);

        var accion = () => turno.Activar(ahora.AddMinutes(16));

        accion.Should().Throw<TurnoExpiradoException>();
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
