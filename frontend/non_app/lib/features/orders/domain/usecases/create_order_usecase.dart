import '../entities/order_draft.dart';
import '../repository/orders_repository.dart';

class CreateOrderUseCase {
  const CreateOrderUseCase(this._repository);

  final OrdersRepository _repository;

  Future<void> call(String id, OrderDraft draft) => _repository.createOrder(id, draft);
}