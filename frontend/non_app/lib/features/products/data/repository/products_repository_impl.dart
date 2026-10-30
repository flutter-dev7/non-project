import '../../domain/entities/product.dart';
import '../../domain/repository/products_repository.dart';
import '../datasources/products_remote_data_source.dart';

class ProductsRepositoryImpl implements ProductsRepository {
  ProductsRepositoryImpl(this._remote);

  final ProductsRemoteDataSource _remote;

  @override
  Future<List<Product>> getProducts() => _remote.getProducts();
}