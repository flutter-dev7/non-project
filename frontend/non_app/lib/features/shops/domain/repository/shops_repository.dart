import '../../../../core/models/paged_result.dart';
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

  Future<PagedResult<Shop>> getShops({
    String? search,
    required int page,
    required int pageSize,
  });

  Future<Shop> createShop({
    required String name,
    required String address,
    required String phone,
    String? contactName,
  });

  Future<ShopBalance> getShopBalance(String id);
}