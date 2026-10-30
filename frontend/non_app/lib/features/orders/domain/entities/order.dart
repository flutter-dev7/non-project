import 'order_status.dart';
import 'payment_type.dart';

class OrderItem {
  const OrderItem({
    required this.productId,
    required this.productName,
    required this.quantity,
    required this.unitPrice,
    required this.lineTotal,
  });

  final String productId;
  final String productName;
  final int quantity;
  final double unitPrice;
  final double lineTotal;
}

class OrderStatusEntry {
  const OrderStatusEntry({
    required this.status,
    required this.changedByName,
    required this.changedAt,
  });

  final OrderStatus status;
  final String? changedByName;
  final DateTime changedAt;
}

class Order {
  const Order({
    required this.id,
    required this.shopId,
    required this.shopName,
    required this.shopAddress,
    required this.shopPhone,
    required this.status,
    required this.deliveryDate,
    required this.paymentType,
    required this.totalAmount,
    required this.courierId,
    required this.courierName,
    required this.source,
    required this.note,
    required this.createdAt,
    required this.items,
    required this.history,
  });

  final String id;
  final String shopId;
  final String shopName;
  final String shopAddress;
  final String shopPhone;
  final OrderStatus status;
  final DateTime deliveryDate;
  final PaymentType paymentType;
  final double totalAmount;
  final String? courierId;
  final String? courierName;
  final String source;
  final String? note;
  final DateTime createdAt;
  final List<OrderItem> items;
  final List<OrderStatusEntry> history;

  bool get canEdit => status == OrderStatus.created;

  bool get canCancel => status == OrderStatus.created || status == OrderStatus.accepted;

  String get number => id.substring(0, 6).toUpperCase();
}