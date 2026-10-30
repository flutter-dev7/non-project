import '../entities/shop.dart';
import '../entities/shop_balance.dart';

abstract class ShopsRepository {
  /// null, если у пользователя ещё нет магазина.
  Future<Shop?> getMyShop();

  Future<Shop> createMyShop({
    required String name,
    required String address,
    required String phone,
    String? contactName,
  });

  Future<ShopBalance> getMyBalance();
}