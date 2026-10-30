import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/utils/formatters.dart';
import '../../../auth/presentation/providers/auth_providers.dart';
import '../../domain/entities/order.dart';
import '../../domain/entities/order_status.dart';
import 'orders_providers.dart';

final courierTodayProvider = FutureProvider.autoDispose<List<Order>>((ref) async {
  final userId = ref.watch(currentUserIdProvider);

  if (userId == null) return [];

  final today = Formatters.today();

  final page = await ref.read(getOrdersUseCaseProvider)(
    courierId: userId,
    dateFrom: today,
    dateTo: today,
    page: 1,
    pageSize: 100,
  );

  final list = page.items.where((o) => o.status != OrderStatus.cancelled).toList();

  int rank(Order o) => o.status == OrderStatus.delivered ? 1 : 0;

  list.sort((a, b) {
    final byDone = rank(a).compareTo(rank(b));
    if (byDone != 0) return byDone;

    final byStatus = b.status.index.compareTo(a.status.index);
    if (byStatus != 0) return byStatus;

    return a.shopName.compareTo(b.shopName);
  });

  return list;
});

final courierHistoryProvider = FutureProvider.autoDispose<List<Order>>((ref) async {
  final userId = ref.watch(currentUserIdProvider);

  if (userId == null) return [];

  final today = Formatters.today();

  final page = await ref.read(getOrdersUseCaseProvider)(
    status: OrderStatus.delivered,
    courierId: userId,
    dateFrom: DateTime(today.year, today.month, today.day - 30),
    dateTo: today,
    page: 1,
    pageSize: 100,
  );

  final list = [...page.items];
  list.sort((a, b) => b.deliveryDate.compareTo(a.deliveryDate));

  return list;
});