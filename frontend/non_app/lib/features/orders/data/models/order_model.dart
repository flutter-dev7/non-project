import '../../../../core/utils/formatters.dart';
import '../../domain/entities/order.dart';
import '../../domain/entities/order_status.dart';
import '../../domain/entities/payment_type.dart';

class OrderModel extends Order {
  const OrderModel({
    required super.id,
    required super.shopId,
    required super.shopName,
    required super.shopAddress,
    required super.shopPhone,
    required super.status,
    required super.deliveryDate,
    required super.paymentType,
    required super.totalAmount,
    required super.courierId,
    required super.courierName,
    required super.source,
    required super.note,
    required super.createdAt,
    required super.items,
    required super.history,
  });

  factory OrderModel.fromJson(Map<String, dynamic> json) {
    final items = (json['items'] as List? ?? const []).map((e) {
      final item = e as Map<String, dynamic>;

      return OrderItem(
        productId: item['productId'] as String,
        productName: item['productName'] as String? ?? '',
        quantity: (item['quantity'] as num? ?? 0).toInt(),
        unitPrice: (item['unitPrice'] as num? ?? 0).toDouble(),
        lineTotal: (item['lineTotal'] as num? ?? 0).toDouble(),
      );
    }).toList();

    final history = (json['history'] as List? ?? const []).map((e) {
      final entry = e as Map<String, dynamic>;

      return OrderStatusEntry(
        status: OrderStatus.parse(entry['status'] as String?),
        changedByName: entry['changedByName'] as String?,
        changedAt: Formatters.parseDateTime(entry['changedAt'] as String),
      );
    }).toList();

    return OrderModel(
      id: json['id'] as String,
      shopId: json['shopId'] as String,
      shopName: json['shopName'] as String? ?? '',
      shopAddress: json['shopAddress'] as String? ?? '',
      shopPhone: json['shopPhone'] as String? ?? '',
      status: OrderStatus.parse(json['status'] as String?),
      deliveryDate: Formatters.parseDate(json['deliveryDate'] as String),
      paymentType: PaymentType.parse(json['paymentType'] as String?),
      totalAmount: (json['totalAmount'] as num? ?? 0).toDouble(),
      courierId: json['courierId'] as String?,
      courierName: json['courierName'] as String?,
      source: json['source'] as String? ?? '',
      note: json['note'] as String?,
      createdAt: Formatters.parseDateTime(json['createdAt'] as String),
      items: items,
      history: history,
    );
  }
}