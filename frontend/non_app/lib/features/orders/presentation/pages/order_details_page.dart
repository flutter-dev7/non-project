import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:non_app/core/widgets/app_dialogs.dart';

import '../../../../core/constants/app_icons.dart';
import '../../../../core/network/api_exception.dart';
import '../../../../core/router/app_routes.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_decor.dart';
import '../../../../core/utils/app_snackbar.dart';
import '../../../../core/utils/error_text.dart';
import '../../../../core/utils/formatters.dart';
import '../../../../core/widgets/app_top_bar.dart';
import '../../../../core/widgets/error_view.dart';
import '../../../../core/widgets/loading_view.dart';
import '../../../shops/presentation/providers/shops_providers.dart';
import '../../domain/entities/order.dart';
import '../providers/orders_providers.dart';
import '../widgets/status_badge.dart';

class OrderDetailsPage extends ConsumerWidget {
  const OrderDetailsPage({super.key, required this.orderId});

  final String orderId;

  Future<void> _cancel(BuildContext context, WidgetRef ref, Order order) async {
    final confirmed = await AppDialogs.confirm(
      context,
      title: 'Отменить заказ?',
      message: 'Заказ на ${Formatters.date(order.deliveryDate)} будет отменён.',
      confirmLabel: 'Отменить заказ',
      destructive: true,
    );

    if (!confirmed) return;

    try {
      await ref.read(cancelOrderUseCaseProvider)(order.id);

      ref.invalidate(orderDetailsProvider(order.id));
      ref.invalidate(ordersProvider);
      ref.invalidate(myBalanceProvider);

      if (context.mounted) {
        AppSnackBar.show(context, 'Заказ отменён');
      }
    } on ApiException catch (e) {
      if (context.mounted) {
        AppSnackBar.show(context, e.message, isError: true);
      }
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final order = ref.watch(orderDetailsProvider(orderId));

    return Scaffold(
      appBar: AppTopBar(title: order.valueOrNull == null ? 'Заказ' : 'Заказ №${order.value!.number}'),
      body: order.when(
        loading: () => const LoadingView(),
        error: (error, _) => ErrorView(
          message: errorText(error),
          onRetry: () => ref.invalidate(orderDetailsProvider(orderId)),
        ),
        data: (data) => ListView(
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
          children: [
            _HeaderCard(order: data),
            const SizedBox(height: 16),
            _InfoCard(order: data),
            const SizedBox(height: 16),
            _ItemsCard(order: data),
            if (data.history.isNotEmpty) ...[
              const SizedBox(height: 16),
              _HistoryCard(order: data),
            ],
            if (data.canEdit || data.canCancel) ...[
              const SizedBox(height: 20),
              if (data.canEdit)
                OutlinedButton.icon(
                  onPressed: () => context.push(AppRoutes.orderForm, extra: data),
                  icon: const Icon(AppIcons.edit, size: 20),
                  label: const Text('Изменить заказ'),
                ),
              if (data.canEdit && data.canCancel) const SizedBox(height: 12),
              if (data.canCancel)
                TextButton(
                  style: TextButton.styleFrom(
                    foregroundColor: AppColors.error,
                    minimumSize: const Size.fromHeight(52),
                  ),
                  onPressed: () => _cancel(context, ref, data),
                  child: const Text('Отменить заказ'),
                ),
            ],
          ],
        ),
      ),
    );
  }
}

class _HeaderCard extends StatelessWidget {
  const _HeaderCard({required this.order});

  final Order order;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: AppDecor.heroGradient,
        borderRadius: BorderRadius.circular(24),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  'Доставка',
                  style: TextStyle(color: Colors.white.withValues(alpha: 0.8)),
                ),
              ),
              Container(
                padding: const EdgeInsets.all(2),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(999),
                ),
                child: StatusBadge(status: order.status),
              ),
            ],
          ),
          const SizedBox(height: 6),
          Text(
            Formatters.date(order.deliveryDate),
            style: const TextStyle(
              fontSize: 22,
              fontWeight: FontWeight.w800,
              color: Colors.white,
            ),
          ),
          const SizedBox(height: 16),
          Text(
            Formatters.money(order.totalAmount),
            style: const TextStyle(
              fontSize: 30,
              fontWeight: FontWeight.w800,
              color: Colors.white,
            ),
          ),
        ],
      ),
    );
  }
}

class _InfoCard extends StatelessWidget {
  const _InfoCard({required this.order});

  final Order order;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Column(
        children: [
          _InfoRow(icon: AppIcons.wallet, label: 'Оплата', value: order.paymentType.label),
          const Divider(indent: 56),
          _InfoRow(
            icon: AppIcons.truck,
            label: 'Курьер',
            value: order.courierName ?? 'Пока не назначен',
          ),
          const Divider(indent: 56),
          _InfoRow(
            icon: AppIcons.calendar,
            label: 'Создан',
            value: Formatters.dateTime(order.createdAt),
          ),
          if (order.note != null && order.note!.isNotEmpty) ...[
            const Divider(indent: 56),
            _InfoRow(icon: AppIcons.note, label: 'Комментарий', value: order.note!),
          ],
        ],
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({required this.icon, required this.label, required this.value});

  final IconData icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 2),
      leading: Icon(icon, color: AppDecor.heroMid),
      title: Text(
        label,
        style: const TextStyle(
          fontSize: 12.5,
          color: AppColors.muted,
          fontWeight: FontWeight.w600,
        ),
      ),
      subtitle: Text(
        value,
        style: const TextStyle(fontSize: 15.5, fontWeight: FontWeight.w700, color: AppColors.brown),
      ),
    );
  }
}

class _ItemsCard extends StatelessWidget {
  const _ItemsCard({required this.order});

  final Order order;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Состав заказа', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 16)),
            const SizedBox(height: 12),
            for (final item in order.items)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 6),
                child: Row(
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(item.productName, style: const TextStyle(fontWeight: FontWeight.w700)),
                          Text(
                            '${item.quantity} × ${Formatters.money(item.unitPrice)}',
                            style: const TextStyle(color: AppColors.muted, fontSize: 13),
                          ),
                        ],
                      ),
                    ),
                    Text(
                      Formatters.money(item.lineTotal),
                      style: const TextStyle(fontWeight: FontWeight.w800),
                    ),
                  ],
                ),
              ),
            const Divider(height: 24),
            Row(
              children: [
                const Text('Итого', style: TextStyle(fontWeight: FontWeight.w800)),
                const Spacer(),
                Text(
                  Formatters.money(order.totalAmount),
                  style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 17),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _HistoryCard extends StatelessWidget {
  const _HistoryCard({required this.order});

  final Order order;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('История', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 16)),
            const SizedBox(height: 12),
            for (final entry in order.history)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 6),
                child: Row(
                  children: [
                    Container(
                      width: 10,
                      height: 10,
                      decoration: BoxDecoration(
                        color: StatusBadge.colorOf(entry.status),
                        shape: BoxShape.circle,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            entry.status.label,
                            style: const TextStyle(fontWeight: FontWeight.w700),
                          ),
                          if (entry.changedByName != null)
                            Text(
                              entry.changedByName!,
                              style: const TextStyle(color: AppColors.muted, fontSize: 13),
                            ),
                        ],
                      ),
                    ),
                    Text(
                      Formatters.dateTime(entry.changedAt),
                      style: const TextStyle(color: AppColors.muted, fontSize: 13),
                    ),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }
}