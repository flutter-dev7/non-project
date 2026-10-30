import '../../../../core/models/paged_result.dart';
import '../../domain/entities/visit.dart';
import '../../domain/repository/visits_repository.dart';
import '../datasources/visits_remote_data_source.dart';

class VisitsRepositoryImpl implements VisitsRepository {
  VisitsRepositoryImpl(this._remote);

  final VisitsRemoteDataSource _remote;

  @override
  Future<PagedResult<Visit>> getVisits({
    DateTime? dateFrom,
    DateTime? dateTo,
    required int page,
    required int pageSize,
  }) async {
    final result = await _remote.getVisits(
      dateFrom: dateFrom,
      dateTo: dateTo,
      page: page,
      pageSize: pageSize,
    );

    return PagedResult<Visit>(items: result.items, hasMore: result.hasMore);
  }

  @override
  Future<void> createVisit({required String shopId, String? note}) {
    return _remote.createVisit(shopId: shopId, note: note);
  }
}