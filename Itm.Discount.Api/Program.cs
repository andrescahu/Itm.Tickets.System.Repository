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
    code = code.Trim().ToUpper();

    var discount = descuentos
        .FirstOrDefault(d => d.Codigo.ToUpper() == code);

    if (discount is null)
        return Results.NotFound(new { message = "Código no encontrado" });

    return Results.Ok(discount);
});

app.Run();