namespace PosSystem.SharedKernel;

public abstract record DomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccuredAt { get; } = DateTime.UtcNow;
    
}

// pushes this event to the queue if there is a low stock in the inventory
public record LowStockEvent(Guid ProductId, Guid BranchId, int currentQty, int RecorderLevel) : DomainEvent;
public record SaleCompletedEvent(Guid SaleId, Guid BranchId, decimal TotalAmount) : DomainEvent;
public record StockTransferredEvent(Guid ProductId, Guid FromBranch, int ToBranch, int qty):DomainEvent;