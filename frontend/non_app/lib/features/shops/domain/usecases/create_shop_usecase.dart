import '../entities/shop.dart';
import '../repository/shops_repository.dart';

class CreateShopUseCase {
  const CreateShopUseCase(this._repository);

  final ShopsRepository _repository;

  Future<Shop> call({
    required String name,
    required String address,
    required String phone,
    String? contactName,
  }) {
    return _repository.createShop(
      name: name,
      address: address,
      phone: phone,
      contactName: contactName,
    );
  }
}