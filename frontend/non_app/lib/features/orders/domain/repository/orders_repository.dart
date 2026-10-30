import '../../../../core/models/paged_result.dart';
import '../entities/order.dart';
import '../entities/order_draft.dart';
import '../entities/order_status.dart';
import '../entities/payment_method.dart';

abstract class OrdersRepository {
  Future<PagedResult<Order>> getOrders({
    OrderStatus? status,
    String? courierId,
    DateTime? dateFrom,
    DateTime? dateTo,
    required int page,
    required int pageSize,
  });

  Future<Order> getOrder(String id);

  Future<void> createOrder(String id, OrderDraft draft);

  Future<void> updateOrder(String id, OrderDraft draft);

  Future<void> cancelOrder(String id);

  Future<void> changeStatus(
    String id,
    OrderStatus status, {
    double? paymentAmount,
    PaymentMethod? paymentMethod,
  });
}