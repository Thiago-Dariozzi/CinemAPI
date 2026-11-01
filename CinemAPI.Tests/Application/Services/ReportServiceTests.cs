using Application.DTOs.Reports;
using Application.Services;
using CinemAPI.Tests.Fakes;
using Domain.Exceptions;
using Domain.Models;

namespace CinemAPI.Tests.Application.Services;

public sealed class ReportServiceTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly FakeReportRepository _reports = new();
    private readonly ReportService _sut;

    public ReportServiceTests() => _sut = new ReportService(_reports);

    [Fact]
    public async Task Sales_AddsUpTheMovies_AndListsTheBestSellingFirst()
    {
        var smaller = Guid.NewGuid();
        var bigger = Guid.NewGuid();
        _reports.Sales.Add(new MovieSalesStat(smaller, "Chica", Purchases: 1, TicketsSold: 2, Subtotal: 20m, FeeAmount: 2m, Total: 22m));
        _reports.Sales.Add(new MovieSalesStat(bigger, "Grande", Purchases: 3, TicketsSold: 5, Subtotal: 50m, FeeAmount: 4.5m, Total: 54.5m));
        _reports.CancelledTickets = 7;

        var report = await _sut.GetSalesAsync(new SalesReportRequest(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)), Ct);

        Assert.Equal((4, 7, 7, 70m, 6.5m, 76.5m),
            (report.Purchases, report.TicketsSold, report.TicketsCancelled, report.TicketRevenue, report.FeesCollected, report.TotalRevenue));
        Assert.Equal([bigger, smaller], report.ByMovie.Select(m => m.MovieId));
        Assert.Equal((new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)), (report.From, report.To));
    }

    // Igual que el filtro de Ventas: los dos días incluidos, completos.
    [Fact]
    public async Task Sales_AsksTheDatabaseForWholeDays_BothIncluded()
    {
        await _sut.GetSalesAsync(new SalesReportRequest(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 1)), Ct);

        Assert.Equal((new DateTime(2026, 10, 1), new DateTime(2026, 10, 2)), _reports.LastPeriod);
    }

    [Fact]
    public async Task Sales_AllowsUpTo366DaysCountingBoth()
    {
        await _sut.GetSalesAsync(new SalesReportRequest(new DateOnly(2025, 10, 1), new DateOnly(2026, 10, 1)), Ct);

        Assert.NotNull(_reports.LastPeriod);
    }

    [Fact]
    public async Task Sales_WithoutSales_ReturnsZeros()
    {
        var report = await _sut.GetSalesAsync(new SalesReportRequest(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 7)), Ct);

        Assert.Equal((0, 0, 0m), (report.TicketsSold, report.Purchases, report.TotalRevenue));
        Assert.Empty(report.ByMovie);
    }

    [Theory]
    [InlineData("2026-10-10", "2026-10-09")]  // desde después de hasta
    [InlineData("2025-09-30", "2026-10-01")]  // 367 días contando los dos
    public async Task Sales_WithAnInvalidRange_ThrowsBusinessRuleException(string from, string to)
    {
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.GetSalesAsync(new SalesReportRequest(DateOnly.Parse(from, System.Globalization.CultureInfo.InvariantCulture), DateOnly.Parse(to, System.Globalization.CultureInfo.InvariantCulture)), Ct));
        Assert.Null(_reports.LastPeriod);
    }

    [Fact]
    public async Task Occupancy_CalculatesThePercentageOfEachShowtime_AndOfTheWholeDay()
    {
        var date = new DateOnly(2026, 10, 27);
        _reports.Seats.Add(Seats(capacity: 100, sold: 25));
        _reports.Seats.Add(Seats(capacity: 30, sold: 10));  // 33,333… → 33,3
        _reports.Seats.Add(Seats(capacity: 0, sold: 0));    // sin butacas: 0 %, no división por cero

        var report = await _sut.GetOccupancyAsync(new OccupancyReportRequest(date), Ct);

        Assert.Equal([25m, 33.3m, 0m], report.Showtimes.Select(s => s.OccupancyPercentage));
        // 35 de 130 butacas, no el promedio de los porcentajes.
        Assert.Equal((130, 35, 26.9m), (report.TotalSeats, report.SoldSeats, report.OccupancyPercentage));
        Assert.Equal((new DateTime(2026, 10, 27), new DateTime(2026, 10, 28)), _reports.LastPeriod);
    }

    [Fact]
    public async Task TheLastRepresentableDay_ThrowsBusinessRuleException_InsteadOfOverflowing()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.GetOccupancyAsync(new OccupancyReportRequest(DateOnly.MaxValue), Ct));
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.GetSalesAsync(new SalesReportRequest(DateOnly.MaxValue.AddDays(-3), DateOnly.MaxValue), Ct));
    }

    [Fact]
    public async Task Occupancy_OfADayWithoutShowtimes_IsZero()
    {
        var report = await _sut.GetOccupancyAsync(new OccupancyReportRequest(new DateOnly(2026, 10, 27)), Ct);

        Assert.Equal((0, 0, 0m), (report.TotalSeats, report.SoldSeats, report.OccupancyPercentage));
        Assert.Empty(report.Showtimes);
    }

    private static ShowtimeSeatsStat Seats(int capacity, int sold) =>
        new(Guid.NewGuid(), new DateTime(2026, 10, 27, 20, 0, 0), Guid.NewGuid(), "Película", Guid.NewGuid(), "Sala 1", capacity, sold);
}
