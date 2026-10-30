import '../../../../core/models/paged_result.dart';
import '../../domain/entities/order.dart';
import '../../domain/entities/order_draft.dart';
import '../../domain/entities/order_status.dart';
import '../../domain/repository/orders_repository.dart';
import '../datasources/orders_remote_data_source.dart';

class OrdersRepositoryImpl implements OrdersRepository {
  OrdersRepositoryImpl(this._remote);

  final OrdersRemoteDataSource _remote;

  @override
  Future<PagedResult<Order>> getOrders({
    OrderStatus? status,
    required int page,
    required int pageSize,
  }) async {
    final result = await _remote.getOrders(status: status, page: page, pageSize: pageSize);

    return PagedResult<Order>(items: result.items, hasMore: result.hasMore);
  }

  @override
  Future<Order> getOrder(String id) => _remote.getOrder(id);

  @override
  Future<void> createOrder(String id, OrderDraft draft) => _remote.createOrder(id, draft);

  @override
  Future<void> updateOrder(String id, OrderDraft draft) => _remote.updateOrder(id, draft);

  @override
  Future<void> cancelOrder(String id) => _remote.cancelOrder(id);
}