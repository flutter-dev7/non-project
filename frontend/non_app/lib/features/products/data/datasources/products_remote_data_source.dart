import '../../../../core/network/api_client.dart';
import '../models/product_model.dart';

class ProductsRemoteDataSource {
  ProductsRemoteDataSource(this._client);

  final ApiClient _client;

  Future<List<ProductModel>> getProducts() async {
    final data = await _client.get('/api/Products');

    final list = data is List ? data : (data as Map<String, dynamic>)['items'] as List;

    return list.map((e) => ProductModel.fromJson(e as Map<String, dynamic>)).toList();
  }
}