using Itm.Discount.Api.Dtos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

var descuentos = new List<DiscountDto>
{
    new("ITM50", 0.5m)
};

app.MapGet("/api/discounts/{code}", (string code) =>
{
    var discount = descuentos
        .FirstOrDefault(d => d.Codigo == code);

    if (discount is null)
        return Results.NotFound();

    return Results.Ok(discount);
});

app.Run();