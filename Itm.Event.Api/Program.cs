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

// GET EVENTO
app.MapGet("/api/events/{id}", (int id) =>
{
    if (id != evento.Id)
        return Results.NotFound();

    return Results.Ok(evento);
});

// RESERVAR
app.MapPost("/api/events/reserve", (ReserveRequest request) =>
{
    if (request.EventId != evento.Id)
        return Results.NotFound();

    if (evento.SillasDisponibles < request.Quantity)
        return Results.BadRequest("No hay suficientes sillas.");

    evento = evento with
    {
        SillasDisponibles = evento.SillasDisponibles - request.Quantity
    };

    return Results.Ok();
});

// LIBERAR (COMPENSACIÓN)
app.MapPost("/api/events/release", (ReserveRequest request) =>
{
    if (request.EventId != evento.Id)
        return Results.NotFound();

    evento = evento with
    {
        SillasDisponibles = evento.SillasDisponibles + request.Quantity
    };

    return Results.Ok();
});

app.Run();