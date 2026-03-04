using Itm.Event.Api.Dtos;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

var evento = new EventDto(
    Id: 1,
    Nombre: "Concierto ITM",
    PrecioBase: 50000,
    SillasDisponibles: 100
);

// Inicializar una variable de capacidad maxima para evitar que la compensar se pase de la cant inicial
var capacidadMaxima = evento.SillasDisponibles;

// GET EVENTO
app.MapGet("/api/events/{id}", (int id) =>
{
    if (id <= 0)
        return Results.BadRequest("El id del evento debe ser mayor a cero.");

    if (id != evento.Id)
        return Results.NotFound("Evento no encontrado en el sistema.");

    return Results.Ok(evento);
});

// RESERVAR
app.MapPost("/api/events/reserve", (ReserveRequest request) =>
{
    if (request is null)
        return Results.BadRequest("El cuerpo de la solicitud es obligatorio.");

    if (request.EventId != evento.Id)
        return Results.NotFound("Evento no encontrado en el sistema.");

    if (request.Quantity <= 0)
        return Results.BadRequest("La cantidad debe ser mayor a cero.");

    if (evento.SillasDisponibles < request.Quantity)
        return Results.Conflict("No hay suficientes sillas disponibles.");

    evento = evento with
    {
        SillasDisponibles = evento.SillasDisponibles - request.Quantity
    };

    return Results.Ok(new
    {
        message = "Reserva realizada con éxito.",
        sillasRestantes = evento.SillasDisponibles
    });
});

// LIBERAR (COMPENSACIÓN)
app.MapPost("/api/events/release", (ReserveRequest request) =>
{
    if (request is null)
        return Results.BadRequest("El cuerpo de la solicitud es obligatorio.");

    if (request.EventId != evento.Id)
        return Results.NotFound("Evento no encontrado.");

    if (request.Quantity <= 0)
        return Results.BadRequest("La cantidad debe ser mayor a cero.");

    if (evento.SillasDisponibles + request.Quantity > capacidadMaxima)
        return Results.Conflict("No se pueden liberar más sillas que la capacidad máxima del evento.");

    evento = evento with
    {
        SillasDisponibles = evento.SillasDisponibles + request.Quantity
    };

    return Results.Ok(new
    {
        message = "Sillas liberadas correctamente.",
        sillasDisponibles = evento.SillasDisponibles
    });
});

app.Run();