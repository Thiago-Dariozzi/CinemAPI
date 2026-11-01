using Application.DTOs.Fees;
using Application.Services;
using CinemAPI.Tests.Fakes;
using Domain.Entities;
using Domain.Exceptions;

namespace CinemAPI.Tests.Application.Services;

public sealed class FeeServiceTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly FakeFeeRepository _fees = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FeeService _sut;

    private readonly Fee _current = new() { Id = Guid.NewGuid(), Name = "Cargo por servicio", Percentage = 10m, IsCurrent = true };
    private readonly Fee _other = new() { Id = Guid.NewGuid(), Name = "Promo", Percentage = 5m, FixedAmount = 100m };

    public FeeServiceTests()
    {
        _fees.Seed(_current, _other);
        _sut = new FeeService(_fees, _unitOfWork);
    }

    [Fact]
    public async Task Create_AsCurrent_UnmarksThePreviousOne()
    {
        var created = await _sut.CreateAsync(new CreateFeeRequest("  Nuevo  ", 12.5m, 50m, IsCurrent: true), Ct);

        Assert.Equal(("Nuevo", 12.5m, 50m, true), (created.Name, created.Percentage, created.FixedAmount, created.IsCurrent));
        Assert.False(_current.IsCurrent);
        Assert.Equal(created.Id, (await _sut.GetCurrentAsync(Ct))!.Id);
        Assert.Equal(2, _unitOfWork.SaveCount); // primero desmarca al anterior, después marca al nuevo
    }

    [Fact]
    public async Task Create_NotCurrent_KeepsThePreviousOne()
    {
        var created = await _sut.CreateAsync(new CreateFeeRequest("Nuevo", 0m, 0m, IsCurrent: false), Ct);

        Assert.False(created.IsCurrent);
        Assert.True(_current.IsCurrent);
    }

    [Fact]
    public async Task Update_MarkingAnotherAsCurrent_UnmarksThePreviousOne()
    {
        await _sut.UpdateAsync(_other.Id, new UpdateFeeRequest("Promo", 5m, 100m, IsCurrent: true), Ct);

        Assert.True(_other.IsCurrent);
        Assert.False(_current.IsCurrent);
    }

    [Fact]
    public async Task Update_TheCurrentOne_KeepingItCurrent_ChangesItsValues()
    {
        await _sut.UpdateAsync(_current.Id, new UpdateFeeRequest("Cargo", 15m, 20m, IsCurrent: true), Ct);

        Assert.Equal(("Cargo", 15m, 20m, true), (_current.Name, _current.Percentage, _current.FixedAmount, _current.IsCurrent));
    }

    [Fact]
    public async Task Update_UnmarkingTheCurrentOne_LeavesNoFee()
    {
        await _sut.UpdateAsync(_current.Id, new UpdateFeeRequest("Cargo por servicio", 10m, 0m, IsCurrent: false), Ct);

        Assert.Null(await _sut.GetCurrentAsync(Ct));
    }

    [Fact]
    public async Task Delete_TheCurrentOne_SoftDeletesItAndLeavesNoFee()
    {
        await _sut.DeleteAsync(_current.Id, Ct);

        Assert.False(_current.IsActive);
        Assert.False(_current.IsCurrent);
        Assert.Null(await _sut.GetCurrentAsync(Ct));
        Assert.Equal([_other.Id], (await _sut.GetAllAsync(Ct)).Select(f => f.Id));
    }

    [Fact]
    public async Task GetAll_ListsTheCurrentOneFirst()
    {
        var aFirst = new Fee { Id = Guid.NewGuid(), Name = "A primero", Percentage = 1m };
        _fees.Seed(aFirst);

        Assert.Equal([_current.Id, aFirst.Id, _other.Id], (await _sut.GetAllAsync(Ct)).Select(f => f.Id));
    }

    [Theory]
    [InlineData(10.555, 0)]
    [InlineData(10, 0.001)]
    public async Task Create_WithMoreThanTwoDecimals_ThrowsBusinessRuleException(double percentage, double fixedAmount)
    {
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.CreateAsync(new CreateFeeRequest("Nuevo", (decimal)percentage, (decimal)fixedAmount, IsCurrent: true), Ct));

        Assert.True(_current.IsCurrent);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    // Dos pedidos simultáneos marcan cada uno su cargo: el índice único filtrado rechaza al segundo.
    [Fact]
    public async Task Create_WhenAnotherFeeBecameCurrentAtTheSameTime_ThrowsFeeConflictException()
    {
        _unitOfWork.OnSave = () => throw new UniqueConstraintConflictException(new InvalidOperationException());

        var ex = await Assert.ThrowsAsync<FeeConflictException>(
            () => _sut.CreateAsync(new CreateFeeRequest("Nuevo", 1m, 0m, IsCurrent: true), Ct));

        Assert.Contains("al mismo tiempo", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateOrDelete_AFeeThatDoesNotExist_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), new UpdateFeeRequest("X", 1m, 0m, IsCurrent: false), Ct));
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid(), Ct));
    }
}
