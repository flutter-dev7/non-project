import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:non_app/core/utils/record_visit.dart';

import '../../../../core/constants/app_icons.dart';
import '../../../../core/router/app_routes.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_decor.dart';
import '../../../../core/utils/error_text.dart';
import '../../../../core/utils/formatters.dart';
import '../../../../core/utils/launcher.dart';
import '../../../../core/widgets/app_top_bar.dart';
import '../../../../core/widgets/inline_message.dart';
import '../../../../core/widgets/primary_button.dart';
import '../../domain/entities/shop.dart';
import '../../domain/entities/shop_balance.dart';
import '../providers/shops_providers.dart';

class ShopDetailsPage extends ConsumerWidget {
  const ShopDetailsPage({super.key, required this.shop});

  final Shop shop;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final balance = ref.watch(shopBalanceProvider(shop.id));

    return Scaffold(
      appBar: AppTopBar(title: shop.name),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(shopBalanceProvider(shop.id));
          await ref.read(shopBalanceProvider(shop.id).future);
        },
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
          children: [
            _InfoCard(shop: shop),
            const SizedBox(height: 16),
            balance.when(
              loading: () => const Padding(
                padding: EdgeInsets.all(32),
                child: Center(child: CircularProgressIndicator()),
              ),
              error: (error, _) => Column(
                children: [
                  InlineMessage(message: errorText(error)),
                  TextButton(
                    onPressed: () => ref.invalidate(shopBalanceProvider(shop.id)),
                    child: const Text('Повторить'),
                  ),
                ],
              ),
              data: (data) => _BalanceSection(balance: data),
            ),
            const SizedBox(height: 20),
            PrimaryButton(
              label: 'Новый заказ',
              icon: AppIcons.cart,
              onPressed: () => context.push(AppRoutes.orderForm, extra: shop),
            ),
            const SizedBox(height: 12),
            OutlinedButton.icon(
              onPressed: () => recordVisit(context, ref, shop),
              icon: const Icon(AppIcons.location, size: 20),
              label: const Text('Отметить визит'),
            ),
          ],
        ),
      ),
    );
  }
}

class _InfoCard extends StatelessWidget {
  const _InfoCard({required this.shop});

  final Shop shop;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    shop.name,
                    style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
                  ),
                  const SizedBox(height: 6),
                  Text(
                    shop.address,
                    style: const TextStyle(color: AppColors.muted, fontWeight: FontWeight.w600),
                  ),
                  const SizedBox(height: 4),
                  Text(shop.phone, style: const TextStyle(fontWeight: FontWeight.w700)),
                  if (shop.contactName != null) ...[
                    const SizedBox(height: 4),
                    Text(
                      'Контакт: ${shop.contactName}',
                      style: const TextStyle(color: AppColors.muted),
                    ),
                  ],
                ],
              ),
            ),
            const SizedBox(width: 12),
            AppIconButton(
              icon: AppIcons.phone,
              tooltip: 'Позвонить',
              onPressed: () => callPhone(context, shop.phone),
            ),
          ],
        ),
      ),
    );
  }
}

class _BalanceSection extends StatelessWidget {
  const _BalanceSection({required this.balance});

  final ShopBalance balance;

  @override
  Widget build(BuildContext context) {
    final limit = balance.debtLimit;
    final overLimit = limit != null && balance.debt > limit;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            gradient: AppDecor.heroGradient,
            borderRadius: BorderRadius.circular(24),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Долг магазина',
                style: TextStyle(color: Colors.white.withValues(alpha: 0.8)),
              ),
              const SizedBox(height: 6),
              Text(
                Formatters.money(balance.debt),
                style: const TextStyle(
                  fontSize: 30,
                  fontWeight: FontWeight.w800,
                  color: Colors.white,
                ),
              ),
              if (limit != null) ...[
                const SizedBox(height: 10),
                Text(
                  overLimit
                      ? 'Превышен лимит ${Formatters.money(limit)}'
                      : 'Лимит долга: ${Formatters.money(limit)}',
                  style: TextStyle(
                    color: overLimit
                        ? const Color(0xFFFFB4A8)
                        : Colors.white.withValues(alpha: 0.85),
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ],
          ),
        ),
        const SizedBox(height: 12),
        Card(
          child: Column(
            children: [
              _Line(label: 'Доставлено на сумму', value: Formatters.money(balance.deliveredTotal)),
              const Divider(),
              _Line(label: 'Оплачено', value: Formatters.money(balance.paidTotal)),
              const Divider(),
              _Line(
                label: 'Заказы в долг в пути',
                value: Formatters.money(balance.pendingDebtOrdersTotal),
              ),
            ],
          ),
        ),
        if (balance.unpaidOrders.isNotEmpty) ...[
          const SizedBox(height: 20),
          const Text(
            'Неоплаченные заказы',
            style: TextStyle(fontSize: 17, fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 10),
          for (final order in balance.unpaidOrders) ...[
            Card(
              child: ListTile(
                contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                leading: const Icon(AppIcons.receipt, color: AppDecor.heroMid),
                title: Text(
                  Formatters.date(order.deliveryDate),
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
            ),
            const SizedBox(height: 8),
          ],
        ],
      ],
    );
  }
}

class _Line extends StatelessWidget {
  const _Line({required this.label, required this.value});

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