namespace Non.Application.Common;

public static class DebtCalculator
{
    public static List<OrderPaymentStatus> Allocate(IEnumerable<DeliveredOrderInfo> orders, decimal paidTotal)
    {
        var remainingPaid = Math.Max(paidTotal, 0m);
        var result = new List<OrderPaymentStatus>();

        foreach (var order in orders
                     .OrderBy(o => o.DeliveryDate)
                     .ThenBy(o => o.CreatedAt)
                     .ThenBy(o => o.Id))
        {
            var paid = Math.Min(order.TotalAmount, remainingPaid);
            remainingPaid -= paid;

            result.Add(new OrderPaymentStatus(order.Id, order.DeliveryDate, order.TotalAmount, paid));
        }

        return result;
    }
}