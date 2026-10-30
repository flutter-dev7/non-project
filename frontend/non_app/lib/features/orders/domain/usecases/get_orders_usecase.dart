import '../../../../core/models/paged_result.dart';
import '../entities/order.dart';
import '../entities/order_status.dart';
import '../repository/orders_repository.dart';

class GetOrdersUseCase {
  const GetOrdersUseCase(this._repository);

  final OrdersRepository _repository;

  Future<PagedResult<Order>> call({
    OrderStatus? status,
    required int page,
    required int pageSize,
  }) {
    return _repository.getOrders(status: status, page: page, pageSize: pageSize);
  }
}