import '../../../../core/models/paged_result.dart';
import '../entities/order.dart';
import '../entities/order_status.dart';
import '../repository/orders_repository.dart';

class GetOrdersUseCase {
  const GetOrdersUseCase(this._repository);

  final OrdersRepository _repository;

  Future<PagedResult<Order>> call({
    OrderStatus? status,
    String? courierId,
    DateTime? dateFrom,
    DateTime? dateTo,
    required int page,
    required int pageSize,
  }) {
    return _repository.getOrders(
      status: status,
      courierId: courierId,
      dateFrom: dateFrom,
      dateTo: dateTo,
      page: page,
      pageSize: pageSize,
    );
  }
}