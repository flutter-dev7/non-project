import '../../../../core/models/paged_result.dart';
import '../../../../core/network/api_exception.dart';
import '../../domain/entities/shop.dart';
import '../../domain/entities/shop_balance.dart';
import '../../domain/repository/shops_repository.dart';
import '../datasources/shops_remote_data_source.dart';

class ShopsRepositoryImpl implements ShopsRepository {
  ShopsRepositoryImpl(this._remote);

  final ShopsRemoteDataSource _remote;

  @override
  Future<Shop?> getMyShop() async {
    try {
      return await _remote.getMyShop();
    } on ApiException catch (e) {
      if (e.type == ApiErrorType.notFound) return null;

      rethrow;
    }
  }

  @override
  Future<Shop> createMyShop({
    required String name,
    required String address,
    required String phone,
    String? contactName,
  }) {
    return _remote.createMyShop(
      name: name,
      address: address,
      phone: phone,
      contactName: contactName,
    );
  }

  @override
  Future<ShopBalance> getMyBalance() => _remote.getMyBalance();

  @override
  Future<PagedResult<Shop>> getShops({
    String? search,
    required int page,
    required int pageSize,
  }) async {
    final result = await _remote.getShops(search: search, page: page, pageSize: pageSize);

    return PagedResult<Shop>(items: result.items, hasMore: result.hasMore);
  }

  @override
  Future<Shop> createShop({
    required String name,
    required String address,
    required String phone,
    String? contactName,
  }) {
    return _remote.createShop(
      name: name,
      address: address,
      phone: phone,
      contactName: contactName,
    );
  }

  @override
  Future<ShopBalance> getShopBalance(String id) => _remote.getShopBalance(id);
}