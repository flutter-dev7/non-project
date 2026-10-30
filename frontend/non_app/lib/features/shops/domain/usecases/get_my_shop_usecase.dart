import '../entities/shop.dart';
import '../repository/shops_repository.dart';

class GetMyShopUseCase {
  const GetMyShopUseCase(this._repository);

  final ShopsRepository _repository;

  Future<Shop?> call() => _repository.getMyShop();
}