import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/constants/app_icons.dart';
import '../../../../core/theme/app_decor.dart';
import '../../../../core/utils/error_text.dart';
import '../../../../core/utils/formatters.dart';
import '../../../../core/widgets/app_top_bar.dart';
import '../../../../core/widgets/empty_state.dart';
import '../../../../core/widgets/error_view.dart';
import '../../../../core/widgets/loading_view.dart';
import '../../domain/entities/order.dart';
import '../../domain/entities/order_status.dart';
import '../../domain/entities/payment_type.dart';
import '../providers/courier_providers.dart';
import '../widgets/courier_order_card.dart';

class CourierTodayPage extends ConsumerWidget {
  const CourierTodayPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final orders = ref.watch(courierTodayProvider);

    return Scaffold(
      appBar: const AppTopBar(title: 'Доставка сегодня', showBack: false),
      body: orders.when(
        loading: () => const LoadingView(),
        error: (error, _) => ErrorView(
          message: errorText(error),
          onRetry: () => ref.invalidate(courierTodayProvider),
        ),
        data: (list) => RefreshIndicator(
          onRefresh: () async {
            ref.invalidate(courierTodayProvider);
            await ref.read(courierTodayProvider.future);
          },
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 20),
            children: [
              _Summary(orders: list),
              const SizedBox(height: 16),
              if (list.isEmpty)
                const Padding(
                  padding: EdgeInsets.only(top: 32),
                  child: EmptyState(
                    icon: AppIcons.truck,
                    title: 'На сегодня заказов нет',
                    message: 'Потяните вниз, чтобы обновить список.',
                  ),
                )
              else
                for (final order in list) ...[
                  CourierOrderCard(order: order),
                  const SizedBox(height: 12),
                ],
            ],
          ),
        ),
      ),
    );
  }
}

class _Summary extends StatelessWidget {
  const _Summary({required this.orders});

  final List<Order> orders;

  @override
  Widget build(BuildContext context) {
    final done = orders.where((o) => o.status == OrderStatus.delivered).length;

    final toCollect = orders
        .where(
          (o) =>
              o.paymentType == PaymentType.now &&
              o.status != OrderStatus.delivered,
        )
        .fold(0.0, (sum, o) => sum + o.totalAmount);

    return Container(
      padding: const EdgeInsets.symmetric(vertical: 20, horizontal: 8),
      decoration: BoxDecoration(
        gradient: AppDecor.heroGradient,
        borderRadius: BorderRadius.circular(24),
      ),
      child: Row(
        children: [
          Expanded(
            flex: 2,
            child: _Stat(label: 'Всего', value: '${orders.length}'),
          ),
          const _StatDivider(),
          Expanded(
            flex: 2,
            child: _Stat(label: 'Доставлено', value: '$done'),
          ),
          const _StatDivider(),
          Expanded(
            flex: 3,
            child: _Stat(label: 'Получить', value: Formatters.money(toCollect)),
          ),
        ],
      ),
    );
  }
}

class _StatDivider extends StatelessWidget {
  const _StatDivider();

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 1,
      height: 40,
      color: Colors.white.withValues(alpha: 0.22),
    );
  }
}

class _Stat extends StatelessWidget {
  const _Stat({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 6),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          FittedBox(
            fit: BoxFit.scaleDown,
            child: Text(
              label,
              style: TextStyle(
                color: Colors.white.withValues(alpha: 0.8),
                fontSize: 13,
              ),
            ),
          ),
          const SizedBox(height: 6),
          FittedBox(
            fit: BoxFit.scaleDown,
            child: Text(
              value,
              style: const TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.w800,
                fontSize: 20,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
