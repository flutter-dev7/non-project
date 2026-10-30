import '../../../../core/utils/formatters.dart';
import '../../domain/entities/shop_balance.dart';

class ShopBalanceModel extends ShopBalance {
  const ShopBalanceModel({
    required super.shopId,
    required super.shopName,
    required super.deliveredTotal,
    required super.paidTotal,
    required super.debt,
    required super.pendingDebtOrdersTotal,
    required super.debtLimit,
    required super.unpaidOrders,
  });

  factory ShopBalanceModel.fromJson(Map<String, dynamic> json) {
    double number(String key) => (json[key] as num? ?? 0).toDouble();

    final unpaid = (json['unpaidOrders'] as List? ?? const []).map((e) {
      final item = e as Map<String, dynamic>;

      return UnpaidOrder(
        orderId: item['orderId'] as String,
        deliveryDate: Formatters.parseDate(item['deliveryDate'] as String),
        totalAmount: (item['totalAmount'] as num? ?? 0).toDouble(),
        paidAmount: (item['paidAmount'] as num? ?? 0).toDouble(),
        remainingAmount: (item['remainingAmount'] as num? ?? 0).toDouble(),
      );
    }).toList();

    return ShopBalanceModel(
      shopId: json['shopId'] as String,
      shopName: json['shopName'] as String? ?? '',
      deliveredTotal: number('deliveredTotal'),
      paidTotal: number('paidTotal'),
      debt: number('debt'),
      pendingDebtOrdersTotal: number('pendingDebtOrdersTotal'),
      debtLimit: (json['debtLimit'] as num?)?.toDouble(),
      unpaidOrders: unpaid,
    );
  }
}