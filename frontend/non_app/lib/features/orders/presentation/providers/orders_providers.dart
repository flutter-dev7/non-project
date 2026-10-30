import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:non_app/features/orders/domain/usecases/change_order_status_usecase.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/providers/core_providers.dart';
import '../../../auth/presentation/providers/auth_providers.dart';
import '../../data/datasources/orders_remote_data_source.dart';
import '../../data/repository/orders_repository_impl.dart';
import '../../domain/entities/order.dart';
import '../../domain/entities/order_draft.dart';
import '../../domain/entities/order_status.dart';
import '../../domain/repository/orders_repository.dart';
import '../../domain/usecases/cancel_order_usecase.dart';
import '../../domain/usecases/create_order_usecase.dart';
import '../../domain/usecases/get_order_usecase.dart';
import '../../domain/usecases/get_orders_usecase.dart';
import '../../domain/usecases/update_order_usecase.dart';

final ordersRepositoryProvider = Provider<OrdersRepository>((ref) {
  return OrdersRepositoryImpl(
    OrdersRemoteDataSource(ref.watch(apiClientProvider)),
  );
});

final getOrdersUseCaseProvider = Provider(
  (ref) => GetOrdersUseCase(ref.watch(ordersRepositoryProvider)),
);
final getOrderUseCaseProvider = Provider(
  (ref) => GetOrderUseCase(ref.watch(ordersRepositoryProvider)),
);
final createOrderUseCaseProvider = Provider(
  (ref) => CreateOrderUseCase(ref.watch(ordersRepositoryProvider)),
);
final updateOrderUseCaseProvider = Provider(
  (ref) => UpdateOrderUseCase(ref.watch(ordersRepositoryProvider)),
);
final cancelOrderUseCaseProvider = Provider(
  (ref) => CancelOrderUseCase(ref.watch(ordersRepositoryProvider)),
);
final changeOrderStatusUseCaseProvider = Provider(
  (ref) => ChangeOrderStatusUseCase(ref.watch(ordersRepositoryProvider)),
);

final orderDetailsProvider = FutureProvider.autoDispose.family<Order, String>((
  ref,
  id,
) {
  return ref.watch(getOrderUseCaseProvider)(id);
});

// Корзина

final cartProvider = NotifierProvider<CartNotifier, Map<String, OrderLine>>(
  CartNotifier.new,
);

class CartNotifier extends Notifier<Map<String, OrderLine>> {
  @override
  Map<String, OrderLine> build() {
    ref.watch(currentUserIdProvider);

    return {};
  }

  void setLine(OrderLine line) {
    final next = Map<String, OrderLine>.of(state);

    if (line.quantity <= 0) {
      next.remove(line.productId);
    } else {
      next[line.productId] = line;
    }

    state = next;
  }

  void replaceAll(Map<String, OrderLine> lines) => state = Map.of(lines);

  void clear() => state = {};
}

extension CartTotals on Map<String, OrderLine> {
  int get totalQuantity => values.fold(0, (sum, line) => sum + line.quantity);

  double get totalAmount => values.fold(0.0, (sum, line) => sum + line.total);
}

// Список заказов с фильтром и подгрузкой страниц

final orderFilterProvider = NotifierProvider<OrderFilterNotifier, OrderStatus?>(
  OrderFilterNotifier.new,
);

class OrderFilterNotifier extends Notifier<OrderStatus?> {
  @override
  OrderStatus? build() => null;

  void select(OrderStatus? status) => state = status;
}

class OrdersState {
  const OrdersState({
    required this.orders,
    required this.page,
    required this.hasMore,
    this.loadingMore = false,
  });

  final List<Order> orders;
  final int page;
  final bool hasMore;
  final bool loadingMore;

  OrdersState copyWith({
    List<Order>? orders,
    int? page,
    bool? hasMore,
    bool? loadingMore,
  }) {
    return OrdersState(
      orders: orders ?? this.orders,
      page: page ?? this.page,
      hasMore: hasMore ?? this.hasMore,
      loadingMore: loadingMore ?? this.loadingMore,
    );
  }
}

const int _ordersPageSize = 20;

final ordersProvider =
    AsyncNotifierProvider.autoDispose<OrdersNotifier, OrdersState>(
      OrdersNotifier.new,
    );

class OrdersNotifier extends AutoDisposeAsyncNotifier<OrdersState> {
  @override
  Future<OrdersState> build() async {
    final status = ref.watch(orderFilterProvider);
    ref.watch(currentUserIdProvider);

    final page = await ref.read(getOrdersUseCaseProvider)(
      status: status,
      page: 1,
      pageSize: _ordersPageSize,
    );

    return OrdersState(orders: page.items, page: 1, hasMore: page.hasMore);
  }

  Future<void> loadMore() async {
    final current = state.valueOrNull;

    if (current == null || !current.hasMore || current.loadingMore) return;

    state = AsyncData(current.copyWith(loadingMore: true));

    try {
      final next = await ref.read(getOrdersUseCaseProvider)(
        status: ref.read(orderFilterProvider),
        page: current.page + 1,
        pageSize: _ordersPageSize,
      );

      state = AsyncData(
        current.copyWith(
          orders: [...current.orders, ...next.items],
          page: current.page + 1,
          hasMore: next.hasMore,
          loadingMore: false,
        ),
      );
    } on ApiException {
      state = AsyncData(current.copyWith(loadingMore: false));
    }
  }
}
