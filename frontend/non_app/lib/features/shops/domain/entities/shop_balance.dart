class UnpaidOrder {
  const UnpaidOrder({
    required this.orderId,
    required this.deliveryDate,
    required this.totalAmount,
    required this.paidAmount,
    required this.remainingAmount,
  });

  final String orderId;
  final DateTime deliveryDate;
  final double totalAmount;
  final double paidAmount;
  final double remainingAmount;
}

class ShopBalance {
  const ShopBalance({
    required this.shopId,
    required this.shopName,
    required this.deliveredTotal,
    required this.paidTotal,
    required this.debt,
    required this.pendingDebtOrdersTotal,
    required this.debtLimit,
    required this.unpaidOrders,
  });

  final String shopId;
  final String shopName;
  final double deliveredTotal;
  final double paidTotal;
  final double debt;
  final double pendingDebtOrdersTotal;
  final double? debtLimit;
  final List<UnpaidOrder> unpaidOrders;
}