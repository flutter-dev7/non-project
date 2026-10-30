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
import '../../../../core/widgets/error_view.dart';
import '../../../../core/widgets/loading_view.dart';
import '../../domain/entities/shop_balance.dart';
import '../providers/shops_providers.dart';

class BalancePage extends ConsumerWidget {
  const BalancePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final balance = ref.watch(myBalanceProvider);

    return Scaffold(
      appBar: const AppTopBar(title: 'Баланс'),
      body: balance.when(
        loading: () => const LoadingView(),
        error: (error, _) => ErrorView(
          message: errorText(error),
          onRetry: () => ref.invalidate(myBalanceProvider),
        ),
        data: (data) => RefreshIndicator(
          onRefresh: () async {
            ref.invalidate(myBalanceProvider);
            await ref.read(myBalanceProvider.future);
          },
          child: ListView(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
            children: [
              _SummaryCard(balance: data),
              const SizedBox(height: 16),
              Card(
                child: Column(
                  children: [
                    _Row(label: 'Доставлено на сумму', value: Formatters.money(data.deliveredTotal)),
                    const Divider(),
                    _Row(label: 'Оплачено', value: Formatters.money(data.paidTotal)),
                    const Divider(),
                    _Row(
                      label: 'Заказы в долг в пути',
                      value: Formatters.money(data.pendingDebtOrdersTotal),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 24),
              const Text(
                'Неоплаченные заказы',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800),
              ),
              const SizedBox(height: 12),
              if (data.unpaidOrders.isEmpty)
                const Card(
                  child: Padding(
                    padding: EdgeInsets.all(20),
                    child: Text(
                      'Все заказы оплачены',
                      style: TextStyle(color: AppColors.muted, fontWeight: FontWeight.w600),
                    ),
                  ),
                )
              else
                for (final order in data.unpaidOrders) ...[
                  _UnpaidTile(order: order),
                  const SizedBox(height: 10),
                ],
            ],
          ),
        ),
      ),
    );
  }
}

class _SummaryCard extends StatelessWidget {
  const _SummaryCard({required this.balance});

  final ShopBalance balance;

  @override
  Widget build(BuildContext context) {
    final limit = balance.debtLimit;
    final overLimit = limit != null && balance.debt > limit;

    return Container(
      padding: const EdgeInsets.all(22),
      decoration: BoxDecoration(
        gradient: AppDecor.heroGradient,
        borderRadius: BorderRadius.circular(24),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Ваш долг',
            style: TextStyle(color: Colors.white.withValues(alpha: 0.8)),
          ),
          const SizedBox(height: 6),
          Text(
            Formatters.money(balance.debt),
            style: const TextStyle(
              fontSize: 32,
              fontWeight: FontWeight.w800,
              color: Colors.white,
            ),
          ),
          if (limit != null) ...[
            const SizedBox(height: 12),
            Text(
              overLimit
                  ? 'Превышен лимит ${Formatters.money(limit)}'
                  : 'Лимит долга: ${Formatters.money(limit)}',
              style: TextStyle(
                color: overLimit ? const Color(0xFFFFB4A8) : Colors.white.withValues(alpha: 0.85),
                fontWeight: FontWeight.w700,
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _Row extends StatelessWidget {
  const _Row({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      child: Row(
        children: [
          Expanded(
            child: Text(
              label,
              style: const TextStyle(color: AppColors.muted, fontWeight: FontWeight.w600),
            ),
          ),
          Text(value, style: const TextStyle(fontWeight: FontWeight.w800)),
        ],
      ),
    );
  }
}

class _UnpaidTile extends StatelessWidget {
  const _UnpaidTile({required this.order});

  final UnpaidOrder order;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
        leading: const Icon(AppIcons.receipt, color: AppDecor.heroMid),
        title: Text(
          'Доставка ${Formatters.date(order.deliveryDate)}',
          style: const TextStyle(fontWeight: FontWeight.w700),
        ),
        subtitle: Text(
          'Оплачено ${Formatters.money(order.paidAmount)} из ${Formatters.money(order.totalAmount)}',
        ),
        trailing: Text(
          Formatters.money(order.remainingAmount),
          style: const TextStyle(fontWeight: FontWeight.w800, color: AppColors.error),
        ),
        onTap: () => context.push(AppRoutes.orderDetailsFor(order.orderId)),
      ),
    );
  }
}