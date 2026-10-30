import '../repository/orders_repository.dart';

class CancelOrderUseCase {
  const CancelOrderUseCase(this._repository);

  final OrdersRepository _repository;

  Future<void> call(String id) => _repository.cancelOrder(id);
}