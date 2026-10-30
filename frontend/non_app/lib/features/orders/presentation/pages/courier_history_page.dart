import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/constants/app_icons.dart';
import '../../../../core/utils/error_text.dart';
import '../../../../core/utils/formatters.dart';
import '../../../../core/widgets/app_top_bar.dart';
import '../../../../core/widgets/empty_state.dart';
import '../../../../core/widgets/error_view.dart';
import '../../../../core/widgets/loading_view.dart';
import '../providers/courier_providers.dart';
import '../widgets/courier_order_card.dart';

class CourierHistoryPage extends ConsumerWidget {
  const CourierHistoryPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final orders = ref.watch(courierHistoryProvider);

    return Scaffold(
      appBar: const AppTopBar(title: 'История', showBack: false),
      body: orders.when(
        loading: () => const LoadingView(),
        error: (error, _) => ErrorView(
          message: errorText(error),
          onRetry: () => ref.invalidate(courierHistoryProvider),
        ),
        data: (list) {
          if (list.isEmpty) {
            return const EmptyState(
              icon: AppIcons.receipt,
              title: 'История пуста',
              message: 'Здесь будут доставки за последние 30 дней.',
            );
          }

          final children = <Widget>[];
          DateTime? lastDate;

          for (final order in list) {
            if (lastDate != order.deliveryDate) {
              lastDate = order.deliveryDate;
              children.add(
                Padding(
                  padding: const EdgeInsets.only(top: 8, bottom: 10),
                  child: Text(
                    Formatters.date(order.deliveryDate),
                    style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 16),
                  ),
                ),
              );
            }

            children.add(CourierOrderCard(order: order, showAction: false));
            children.add(const SizedBox(height: 12));
          }

          return RefreshIndicator(
            onRefresh: () async {
              ref.invalidate(courierHistoryProvider);
              await ref.read(courierHistoryProvider.future);
            },
            child: ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 20),
              children: children,
            ),
          );
        },
      ),
    );
  }
}