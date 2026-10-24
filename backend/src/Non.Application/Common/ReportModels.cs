namespace Non.Application.Common;

public record ProductionRow(Guid ProductId, string ProductName, int Quantity);

public record SalesDayRow(DateOnly Date, int OrdersCount, decimal Amount);

public record SalesProductRow(Guid ProductId, string ProductName, int Quantity, decimal Amount);