using Orders.Domain.Orders;

namespace Orders.Api;

internal static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/orders").WithTags("Orders");

        orders.MapPost("/", PlaceOrder)
            .Produces<OrderResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        orders.MapGet("/{id:guid}", GetOrder)
            .Produces<OrderResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        orders.MapPost("/{id:guid}/confirm", (Guid id, IOrderRepository repo, CancellationToken ct) =>
                ChangeStatus(id, repo, order => order.Confirm(), ct))
            .Produces<OrderResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        orders.MapPost("/{id:guid}/cancel", (Guid id, IOrderRepository repo, CancellationToken ct) =>
                ChangeStatus(id, repo, order => order.Cancel(), ct))
            .Produces<OrderResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> PlaceOrder(PlaceOrderRequest request, IOrderRepository repo, CancellationToken ct)
    {
        // Domain rules (at least one line, positive quantity, one currency) throw DomainException -> 400.
        var lines = (request.Lines ?? [])
            .Select(l => new OrderLine(l.ProductId, l.Quantity, new Money(l.UnitPrice, l.Currency)));
        var order = Order.Place(request.CustomerId, lines);

        await repo.AddAsync(order, ct);
        await repo.SaveChangesAsync(ct);

        return Results.Created($"/orders/{order.Id}", OrderResponse.From(order));
    }

    private static async Task<IResult> GetOrder(Guid id, IOrderRepository repo, CancellationToken ct)
    {
        var order = await repo.GetAsync(id, ct);
        return order is null ? NotFound(id) : Results.Ok(OrderResponse.From(order));
    }

    private static async Task<IResult> ChangeStatus(Guid id, IOrderRepository repo, Action<Order> change, CancellationToken ct)
    {
        var order = await repo.GetAsync(id, ct);
        if (order is null)
            return NotFound(id);

        change(order);
        await repo.SaveChangesAsync(ct);
        return Results.Ok(OrderResponse.From(order));
    }

    private static IResult NotFound(Guid id) =>
        Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Order not found", detail: $"No order with id {id}.");
}
