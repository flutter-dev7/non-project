import '../entities/order_draft.dart';
import '../repository/orders_repository.dart';

class UpdateOrderUseCase {
  const UpdateOrderUseCase(this._repository);

  final OrdersRepository _repository;

  Future<void> call(String id, OrderDraft draft) => _repository.updateOrder(id, draft);
}