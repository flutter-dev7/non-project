import '../../../../core/network/api_client.dart';
import '../models/shop_balance_model.dart';
import '../models/shop_model.dart';

class ShopsRemoteDataSource {
  ShopsRemoteDataSource(this._client);

  final ApiClient _client;

  Future<ShopModel> getMyShop() async {
    final data = await _client.get('/api/Shops/my');

    return ShopModel.fromJson(data as Map<String, dynamic>);
  }

  Future<ShopModel> createMyShop({
    required String name,
    required String address,
    required String phone,
    String? contactName,
  }) async {
    final data = await _client.post(
      '/api/Shops/my',
      data: {
        'name': name,
        'address': address,
        'phone': phone,
        if (contactName != null) 'contactName': contactName,
      },
    );

    return ShopModel.fromJson(data as Map<String, dynamic>);
  }

  Future<ShopBalanceModel> getMyBalance() async {
    final data = await _client.get('/api/Shops/my/balance');

    return ShopBalanceModel.fromJson(data as Map<String, dynamic>);
  }
}