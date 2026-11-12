using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orders.Api;
using Orders.IntegrationTests.Infrastructure;

namespace Orders.IntegrationTests;

[Collection(InfrastructureCollection.Name)]
public sealed class OrderApiTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private ApiFactory _api = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(infrastructure);
        _client = _api.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private static PlaceOrderRequest ValidRequest() =>
        new(Guid.NewGuid(), [new PlaceOrderLine(Guid.NewGuid(), 2, 19.99m, "EUR")]);

    private async Task<OrderResponse> PlaceAsync()
    {
        var response = await _client.PostAsJsonAsync("/orders", ValidRequest());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<OrderResponse>())!;
    }

    [Fact]
    public async Task Placing_an_order_writes_the_order_and_an_outbox_message()
    {
        var order = await PlaceAsync();

        order.Status.Should().Be("Pending");
        order.Total.Should().Be(39.98m);

        await using var db = infrastructure.CreateDbContext();
        (await db.Orders.AnyAsync(o => o.Id == order.Id)).Should().BeTrue();

        var placedMessages = await db.OutboxMessages.Where(m => m.Type == "OrderPlaced").ToListAsync();
        placedMessages.Where(m => m.Payload.Contains(order.Id.ToString())).Should().ContainSingle();
    }

    [Fact]
    public async Task Get_returns_the_placed_order()
    {
        var order = await PlaceAsync();

        var fetched = await _client.GetFromJsonAsync<OrderResponse>($"/orders/{order.Id}");

        fetched!.Id.Should().Be(order.Id);
        fetched.Lines.Should().ContainSingle();
    }

    [Fact]
    public async Task A_cancelled_order_cannot_be_confirmed()
    {
        var order = await PlaceAsync();
        (await _client.PostAsync($"/orders/{order.Id}/cancel", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await _client.PostAsync($"/orders/{order.Id}/confirm", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Detail.Should().Contain("cancelled order cannot be confirmed");
    }

    [Fact]
    public async Task An_order_without_lines_is_rejected_with_400()
    {
        var response = await _client.PostAsJsonAsync("/orders", new PlaceOrderRequest(Guid.NewGuid(), []));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_unknown_order_returns_404()
    {
        var response = await _client.GetAsync($"/orders/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
