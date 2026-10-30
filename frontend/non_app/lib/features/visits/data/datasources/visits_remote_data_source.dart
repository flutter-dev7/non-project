import '../../../../core/models/paged_result.dart';
import '../../../../core/network/api_client.dart';
import '../../../../core/utils/formatters.dart';
import '../models/visit_model.dart';

class VisitsRemoteDataSource {
  VisitsRemoteDataSource(this._client);

  final ApiClient _client;

  Future<PagedResult<VisitModel>> getVisits({
    DateTime? dateFrom,
    DateTime? dateTo,
    required int page,
    required int pageSize,
  }) async {
    final path = Uri(
      path: '/api/Visits',
      queryParameters: {
        'Page': '$page',
        'PageSize': '$pageSize',
        if (dateFrom != null) 'DateFrom': Formatters.apiDate(dateFrom),
        if (dateTo != null) 'DateTo': Formatters.apiDate(dateTo),
      },
    ).toString();

    final data = await _client.get(path);

    return PagedResult.fromJson(data, VisitModel.fromJson, page: page, pageSize: pageSize);
  }

  Future<void> createVisit({required String shopId, String? note}) async {
    await _client.post(
      '/api/Visits',
      data: {'shopId': shopId, if (note != null) 'note': note},
    );
  }
}