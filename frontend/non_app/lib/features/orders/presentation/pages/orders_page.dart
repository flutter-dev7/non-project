import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/constants/app_icons.dart';
import '../../../../core/router/app_routes.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_decor.dart';
import '../../../../core/utils/error_text.dart';
import '../../../../core/utils/formatters.dart';
import '../../../../core/widgets/app_top_bar.dart';
import '../../../../core/widgets/empty_state.dart';
import '../../../../core/widgets/error_view.dart';
import '../../../../core/widgets/loading_view.dart';
import '../../../shops/presentation/providers/shops_providers.dart';
import '../../../shops/presentation/widgets/shop_gate.dart';
import '../../domain/entities/order_status.dart';
import '../providers/orders_providers.dart';
import '../widgets/order_card.dart';

class OrdersPage extends StatelessWidget {
  const OrdersPage({super.key});

  @override
  Widget build(BuildContext context) {
    return const ShopGate(title: 'Заказы', child: _OrdersView());
  }
}

class _OrdersView extends ConsumerStatefulWidget {
  const _OrdersView();

  @override
  ConsumerState<_OrdersView> createState() => _OrdersViewState();
}

class _OrdersViewState extends ConsumerState<_OrdersView> {
  final _scroll = ScrollController();

  @override
  void initState() {
    super.initState();
    _scroll.addListener(_onScroll);
  }

  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_scroll.position.pixels >= _scroll.position.maxScrollExtent - 240) {
      ref.read(ordersProvider.notifier).loadMore();
    }
  }

  Future<void> _refresh() async {
    ref.invalidate(myBalanceProvider);
    ref.invalidate(ordersProvider);
    await ref.read(ordersProvider.future);
  }

  @override
  Widget build(BuildContext context) {
    final orders = ref.watch(ordersProvider);

    return Scaffold(
      appBar: const AppTopBar(title: 'Мои заказы', showBack: false),
      body: orders.when(
        loading: () => const LoadingView(),
        error: (error, _) => ErrorView(
          message: errorText(error),
          onRetry: () => ref.invalidate(ordersProvider),
        ),
        data: (state) => RefreshIndicator(
          onRefresh: _refresh,
          child: ListView.builder(
            controller: _scroll,
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.fromLTRB(20, 0, 20, 20),
            itemCount: state.orders.length + 2,
            itemBuilder: (context, index) {
              if (index == 0) return const _Header();

              if (index == state.orders.length + 1) {
                if (state.orders.isEmpty) {
                  return const Padding(
                    padding: EdgeInsets.only(top: 48),
                    child: EmptyState(
                      icon: AppIcons.receipt,
                      title: 'Заказов пока нет',
                      message: 'Оформите первый заказ на вкладке «Заказать».',
                    ),
                  );
                }

                return state.loadingMore
                    ? const Padding(
                        padding: EdgeInsets.all(16),
                        child: Center(child: CircularProgressIndicator()),
                      )
                    : const SizedBox(height: 8);
              }

              return Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: OrderCard(order: state.orders[index - 1]),
              );
            },
          ),
        ),
      ),
    );
  }
}

class _Header extends ConsumerWidget {
  const _Header();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final balance = ref.watch(myBalanceProvider).valueOrNull;
    final filter = ref.watch(orderFilterProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const SizedBox(height: 8),
        if (balance != null)
          Material(
            color: Colors.transparent,
            child: InkWell(
              borderRadius: BorderRadius.circular(20),
              onTap: () => context.push(AppRoutes.balance),
              child: Ink(
                padding: const EdgeInsets.all(18),
                decoration: BoxDecoration(
                  gradient: AppDecor.heroGradient,
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Row(
                  children: [
                    const Icon(AppIcons.wallet, color: Colors.white, size: 28),
                    const SizedBox(width: 14),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Ваш долг',
                            style: TextStyle(color: Colors.white.withValues(alpha: 0.8)),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            Formatters.money(balance.debt),
                            style: const TextStyle(
                              color: Colors.white,
                              fontWeight: FontWeight.w800,
                              fontSize: 20,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const Icon(AppIcons.forward, color: Colors.white, size: 18),
                  ],
                ),
              ),
            ),
          ),
        const SizedBox(height: 16),
        SizedBox(
          height: 40,
          child: ListView(
            scrollDirection: Axis.horizontal,
            children: [
              _FilterChip(label: 'Все', selected: filter == null, status: null),
              for (final status in OrderStatus.values)
                _FilterChip(label: status.label, selected: filter == status, status: status),
            ],
          ),
        ),
        const SizedBox(height: 16),
      ],
    );
  }
}

class _FilterChip extends ConsumerWidget {
  const _FilterChip({required this.label, required this.selected, required this.status});

  final String label;
  final bool selected;
  final OrderStatus? status;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Padding(
      padding: const EdgeInsets.only(right: 8),
      child: ChoiceChip(
        label: Text(label),
        selected: selected,
        showCheckmark: false,
        selectedColor: AppDecor.heroMid,
        backgroundColor: Colors.white,
        side: BorderSide(color: selected ? AppDecor.heroMid : AppColors.border),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(999)),
        labelStyle: TextStyle(
          fontWeight: FontWeight.w700,
          color: selected ? Colors.white : AppColors.brown,
        ),
        onSelected: (_) => ref.read(orderFilterProvider.notifier).select(status),
      ),
    );
  }
}