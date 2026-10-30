import '../../../../core/models/paged_result.dart';
import '../entities/visit.dart';
import '../repository/visits_repository.dart';

class GetVisitsUseCase {
  const GetVisitsUseCase(this._repository);

  final VisitsRepository _repository;

  Future<PagedResult<Visit>> call({
    DateTime? dateFrom,
    DateTime? dateTo,
    required int page,
    required int pageSize,
  }) {
    return _repository.getVisits(
      dateFrom: dateFrom,
      dateTo: dateTo,
      page: page,
      pageSize: pageSize,
    );
  }
}