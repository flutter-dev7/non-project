import '../../../../core/models/paged_result.dart';
import '../entities/visit.dart';

abstract class VisitsRepository {
  Future<PagedResult<Visit>> getVisits({
    DateTime? dateFrom,
    DateTime? dateTo,
    required int page,
    required int pageSize,
  });

  Future<void> createVisit({required String shopId, String? note});
}