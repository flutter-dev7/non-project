import '../entities/order_status.dart';
import '../entities/payment_method.dart';
import '../repository/orders_repository.dart';

class ChangeOrderStatusUseCase {
  const ChangeOrderStatusUseCase(this._repository);

  final OrdersRepository _repository;

  Future<void> call(
    String id,
    OrderStatus status, {
    double? paymentAmount,
    PaymentMethod? paymentMethod,
  }) {
    return _repository.changeStatus(
      id,
      status,
      paymentAmount: paymentAmount,
      paymentMethod: paymentMethod,
    );
  }
}