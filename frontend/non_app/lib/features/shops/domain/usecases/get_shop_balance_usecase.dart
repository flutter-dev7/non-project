import '../entities/shop_balance.dart';
import '../repository/shops_repository.dart';

class GetShopBalanceUseCase {
  const GetShopBalanceUseCase(this._repository);

  final ShopsRepository _repository;

  Future<ShopBalance> call(String id) => _repository.getShopBalance(id);
}