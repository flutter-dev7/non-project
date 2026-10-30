import '../entities/product.dart';
import '../repository/products_repository.dart';

class GetProductsUseCase {
  const GetProductsUseCase(this._repository);

  final ProductsRepository _repository;

  Future<List<Product>> call() => _repository.getProducts();
}