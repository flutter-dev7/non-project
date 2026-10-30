import '../../../../core/models/paged_result.dart';
import '../entities/shop.dart';
import '../repository/shops_repository.dart';

class GetShopsUseCase {
  const GetShopsUseCase(this._repository);

  final ShopsRepository _repository;

  Future<PagedResult<Shop>> call({
    String? search,
    required int page,
    required int pageSize,
  }) {
    return _repository.getShops(search: search, page: page, pageSize: pageSize);
  }
}