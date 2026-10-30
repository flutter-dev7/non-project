import '../entities/order.dart';
import '../repository/orders_repository.dart';

class GetOrderUseCase {
  const GetOrderUseCase(this._repository);

  final OrdersRepository _repository;

  Future<Order> call(String id) => _repository.getOrder(id);
}