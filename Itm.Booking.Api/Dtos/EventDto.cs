namespace Itm.Event.Api.Dtos;

public record EventDto(
    int Id,
    string Nombre,
    decimal PrecioBase,
    int SillasDisponibles
);