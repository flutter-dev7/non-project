import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/constants/app_icons.dart';
import '../../../../core/router/app_routes.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/utils/formatters.dart';
import '../../../../core/utils/launcher.dart';
import '../../../../core/widgets/app_top_bar.dart';
import '../../domain/entities/order.dart';
import '../../domain/entities/payment_type.dart';
import 'courier_action_button.dart';
import 'status_badge.dart';

class CourierOrderCard extends StatelessWidget {
  const CourierOrderCard({super.key, required this.order, this.showAction = true});

  final Order order;
  final bool showAction;

  int get _pieces => order.items.fold(0, (sum, item) => sum + item.quantity);

  @override
  Widget build(BuildContext context) {
    final hasAction = showAction && nextCourierStatus(order) != null;

    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(20),
        onTap: () => context.push(AppRoutes.orderDetailsFor(order.id)),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      order.shopName,
                      style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 17),
                    ),
                  ),
                  StatusBadge(status: order.status),
                ],
              ),
              const SizedBox(height: 8),
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Icon(AppIcons.location, size: 18, color: AppColors.muted),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Text(
                      order.shopAddress,
                      style: const TextStyle(
                        color: AppColors.muted,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Text(
                    '$_pieces шт.',
                    style: const TextStyle(fontWeight: FontWeight.w700),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Text(
                      order.paymentType == PaymentType.now
                          ? 'Оплата сразу'
                          : 'В долг',
                      style: const TextStyle(
                        color: AppColors.muted,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                  Text(
                    Formatters.money(order.totalAmount),
                    style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 17),
                  ),
                ],
              ),
              if (hasAction) ...[
                const SizedBox(height: 14),
                Row(
                  children: [
                    AppIconButton(
                      icon: AppIcons.phone,
                      tooltip: 'Позвонить',
                      onPressed: () => callPhone(context, order.shopPhone),
                    ),
                    const SizedBox(width: 12),
                    Expanded(child: CourierActionButton(order: order)),
                  ],
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}