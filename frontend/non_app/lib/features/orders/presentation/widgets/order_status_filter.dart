import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_decor.dart';
import '../../domain/entities/order_status.dart';
import '../providers/orders_providers.dart';

class OrderStatusFilter extends ConsumerWidget {
  const OrderStatusFilter({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final filter = ref.watch(orderFilterProvider);

    return SizedBox(
      height: 40,
      child: ListView(
        scrollDirection: Axis.horizontal,
        children: [
          _Chip(label: 'Все', selected: filter == null, status: null),
          for (final status in OrderStatus.values)
            _Chip(label: status.label, selected: filter == status, status: status),
        ],
      ),
    );
  }
}

class _Chip extends ConsumerWidget {
  const _Chip({required this.label, required this.selected, required this.status});

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