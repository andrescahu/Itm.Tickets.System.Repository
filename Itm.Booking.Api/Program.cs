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
    var eventClient = factory.CreateClient("EventClient");
    var discountClient = factory.CreateClient("DiscountClient");

    try
    {
        // LECTURA EN PARALELO
        var eventTask = eventClient.GetFromJsonAsync<EventDto>($"/api/events/{request.EventId}");
        var discountTask = discountClient.GetAsync($"/api/discounts/{request.DiscountCode}");

        await Task.WhenAll(eventTask, discountTask);

        var evento = await eventTask;

        DiscountDto? discount = null;

        if (discountTask.Result.IsSuccessStatusCode)
        {
            discount = await discountTask.Result.Content
                .ReadFromJsonAsync<DiscountDto>();
        }

        if (evento is null)
            return Results.NotFound("Evento no existe.");

        // CALCULAR TOTAL
        decimal total = evento.PrecioBase * request.Tickets;

        if (discount is not null)
            total -= total * discount.Porcentaje;

        // RESERVAR (INICIO SAGA)
        var reserveResponse = await eventClient.PostAsJsonAsync(
            "/api/events/reserve",
            new { EventId = request.EventId, Quantity = request.Tickets });

        if (!reserveResponse.IsSuccessStatusCode)
            return Results.BadRequest("No hay sillas suficientes.");

        try
        {
            // SIMULACIÓN PAGO
            bool paymentSuccess = new Random().Next(1, 10) > 5;

            if (!paymentSuccess)
                throw new Exception("Pago rechazado");

            return Results.Ok(new
            {
                Status = "Éxito",
                TotalPagado = total,
                Message = "¡Disfruta el concierto ITM!"
            });
        }
        catch
        {
            // COMPENSACIÓN
            await eventClient.PostAsJsonAsync(
                "/api/events/release",
                new { EventId = request.EventId, Quantity = request.Tickets });

            return Results.Problem(
                "Tu pago fue rechazado. No te cobramos y tus sillas fueron liberadas.");
        }
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error inesperado: {ex.Message}");
    }
});

app.Run();