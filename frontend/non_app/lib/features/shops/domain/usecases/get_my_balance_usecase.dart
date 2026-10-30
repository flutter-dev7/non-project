import '../entities/shop_balance.dart';
import '../repository/shops_repository.dart';

class GetMyBalanceUseCase {
  const GetMyBalanceUseCase(this._repository);

  final ShopsRepository _repository;

  Future<ShopBalance> call() => _repository.getMyBalance();
}