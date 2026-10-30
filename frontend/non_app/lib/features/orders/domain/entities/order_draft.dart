import 'payment_type.dart';

class OrderLine {
  const OrderLine({
    required this.productId,
    required this.name,
    required this.unitPrice,
    required this.quantity,
  });

  final String productId;
  final String name;
  final double unitPrice;
  final int quantity;

  double get total => unitPrice * quantity;

  OrderLine withQuantity(int value) => OrderLine(
        productId: productId,
        name: name,
        unitPrice: unitPrice,
        quantity: value,
      );
}

class OrderDraft {
  const OrderDraft({
    required this.deliveryDate,
    required this.paymentType,
    required this.note,
    required this.lines,
    this.shopId,
  });

  final DateTime deliveryDate;
  final PaymentType paymentType;
  final String? note;
  final List<OrderLine> lines;

  /// Заполняется, когда заказ оформляет агент за магазин.
  final String? shopId;
}