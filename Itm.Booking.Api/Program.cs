using Itm.Booking.Api.Dtos;
using Itm.Discount.Api.Dtos;
using Itm.Event.Api.Dtos;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CLIENTE EVENT
builder.Services.AddHttpClient("EventClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5169"); // Puerto Event.Api
})
.AddStandardResilienceHandler();

// CLIENTE DISCOUNT
builder.Services.AddHttpClient("DiscountClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5243"); // Puerto Discount.Api
})
.AddStandardResilienceHandler();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/api/bookings", async (BookingRequest request, IHttpClientFactory factory) =>
{
    if (request is null)
        return Results.BadRequest("El cuerpo de la solicitud es obligatorio.");

    if (request.EventId <= 0)
        return Results.BadRequest("Código de Evento inválido, intente nuevamente con un código valido.");

    if (request.Tickets <= 0)
        return Results.BadRequest("La cantidad de tickets debe ser mayor a cero.");

    // Maximo de tickets por compra
    var maxTicketsPerBooking = 5;

    if (request.Tickets > maxTicketsPerBooking)
        return Results.BadRequest(
            $"No se pueden comprar más de {maxTicketsPerBooking} boletas por reserva.");

    var eventClient = factory.CreateClient("EventClient");
    var discountClient = factory.CreateClient("DiscountClient");

    try
    {
        // LECTURA EN PARALELO (OBLIGATORIA)
        var eventTask = eventClient
            .GetFromJsonAsync<EventDto>($"/api/events/{request.EventId}");

        Task<HttpResponseMessage> discountTask;

        if (string.IsNullOrWhiteSpace(request.DiscountCode))
        {
            // Si no hay código, simulamos respuesta 404
            discountTask = Task.FromResult(
                new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        }
        else
        {
            discountTask = discountClient
                .GetAsync($"/api/discounts/{request.DiscountCode}");
        }

        await Task.WhenAll(eventTask, discountTask);

        var evento = await eventTask;

        if (evento is null)
            return Results.NotFound("El Evento no existe.");

        DiscountDto? discount = null;

        if (discountTask.Result.IsSuccessStatusCode)
        {
            discount = await discountTask.Result.Content
                .ReadFromJsonAsync<DiscountDto>();
        }

        // Calcular total
        decimal subtotal = evento.PrecioBase * request.Tickets;

        decimal discountAmount = 0;

        if (discount is not null)
        {
            discountAmount = subtotal * discount.Porcentaje;
        }

        decimal total = subtotal - discountAmount;

        // Reservar (Inicio SAGA)
        var reserveResponse = await eventClient.PostAsJsonAsync(
            "/api/events/reserve",
            new { EventId = request.EventId, Quantity = request.Tickets });

        if (reserveResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
            return Results.Conflict("No hay sillas suficientes, intente de nuevo con un valor menor.");

        if (!reserveResponse.IsSuccessStatusCode)
            return Results.Problem("Error al reservar las sillas.");

        // Simulación de pago
        bool paymentSuccess = new Random().Next(1, 10) > 5;

        if (!paymentSuccess)
        {
            // COMPENSACIÓN
            await eventClient.PostAsJsonAsync(
                "/api/events/release",
                new { EventId = request.EventId, Quantity = request.Tickets });

            return Results.BadRequest(
                "Pago rechazado. No se realizó el cobro y las sillas fueron liberadas.");
        }

        return Results.Ok(new
        {
            Status = "Compra Éxitosa",
            Evento = evento.Nombre,
            PrecioUnitario = evento.PrecioBase,
            CantidadBoletas = request.Tickets,
            Subtotal = subtotal,
            PorcentajeDescuento = discount?.Porcentaje ?? 0,
            ValorDescuento = discountAmount,
            TotalPagado = total,
            FechaCompra = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
            Message = "¡Tus boletas seran enviadas a continuación, Disfruta del concierto ITM!"
        });
    }
    catch
    {
        return Results.Problem("Error inesperado en el proceso de reserva.");
    }
});

app.Run();