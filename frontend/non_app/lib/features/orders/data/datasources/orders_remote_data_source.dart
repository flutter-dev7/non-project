import '../../../../core/models/paged_result.dart';
import '../../../../core/network/api_client.dart';
import '../../../../core/utils/formatters.dart';
import '../../domain/entities/order_draft.dart';
import '../../domain/entities/order_status.dart';
import '../../domain/entities/payment_method.dart';
import '../models/order_model.dart';

class OrdersRemoteDataSource {
  OrdersRemoteDataSource(this._client);

  final ApiClient _client;

  Future<PagedResult<OrderModel>> getOrders({
    OrderStatus? status,
    String? courierId,
    DateTime? dateFrom,
    DateTime? dateTo,
    required int page,
    required int pageSize,
  }) async {
    final path = Uri(
      path: '/api/Orders',
      queryParameters: {
        'Page': '$page',
        'PageSize': '$pageSize',
        if (status != null) 'Status': status.apiName,
        if (courierId != null) 'CourierId': courierId,
        if (dateFrom != null) 'DeliveryDateFrom': Formatters.apiDate(dateFrom),
        if (dateTo != null) 'DeliveryDateTo': Formatters.apiDate(dateTo),
      },
    ).toString();

    final data = await _client.get(path);

    return PagedResult.fromJson(
      data,
      OrderModel.fromJson,
      page: page,
      pageSize: pageSize,
    );
  }

  Future<OrderModel> getOrder(String id) async {
    final data = await _client.get('/api/Orders/$id');

    return OrderModel.fromJson(data as Map<String, dynamic>);
  }

  Map<String, dynamic> _body(OrderDraft draft) => {
    'deliveryDate': Formatters.apiDate(draft.deliveryDate),
    'paymentType': draft.paymentType.apiName,
    if (draft.note != null) 'note': draft.note,
    'items': [
      for (final line in draft.lines)
        {'productId': line.productId, 'quantity': line.quantity},
    ],
  };

  Future<void> createOrder(String id, OrderDraft draft) async {
    await _client.post(
      '/api/Orders',
      data: {
        'id': id,
        if (draft.shopId != null) 'shopId': draft.shopId,
        ..._body(draft),
      },
    );
  }

  Future<void> updateOrder(String id, OrderDraft draft) async {
    await _client.put('/api/Orders/$id', data: _body(draft));
  }

  Future<void> cancelOrder(String id) async {
    await _client.post('/api/Orders/$id/cancel');
  }

  Future<void> changeStatus(
    String id,
    OrderStatus status, {
    double? paymentAmount,
    PaymentMethod? paymentMethod,
  }) async {
    await _client.put(
      '/api/Orders/$id/status',
      data: {
        'status': status.apiName,
        if (paymentAmount != null) 'paymentAmount': paymentAmount,
        if (paymentMethod != null) 'paymentMethod': paymentMethod.apiName,
      },
    );
  }
}
