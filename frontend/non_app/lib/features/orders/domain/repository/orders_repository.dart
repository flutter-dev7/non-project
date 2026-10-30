import '../../../../core/models/paged_result.dart';
import '../entities/order.dart';
import '../entities/order_draft.dart';
import '../entities/order_status.dart';

abstract class OrdersRepository {
  Future<PagedResult<Order>> getOrders({
    OrderStatus? status,
    required int page,
    required int pageSize,
  });

  Future<Order> getOrder(String id);

  Future<void> createOrder(String id, OrderDraft draft);

  Future<void> updateOrder(String id, OrderDraft draft);

  Future<void> cancelOrder(String id);
}