import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:non_app/core/utils/record_visit.dart';

import '../../../../core/constants/app_icons.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_decor.dart';
import '../../../../core/utils/error_text.dart';
import '../../../../core/utils/formatters.dart';
import '../../../../core/widgets/app_top_bar.dart';
import '../../../../core/widgets/empty_state.dart';
import '../../../../core/widgets/error_view.dart';
import '../../../../core/widgets/loading_view.dart';
import '../../../shops/presentation/widgets/shop_picker.dart';
import '../../domain/entities/visit.dart';
import '../providers/visits_providers.dart';

class VisitsPage extends ConsumerWidget {
  const VisitsPage({super.key});

  Future<void> _newVisit(BuildContext context, WidgetRef ref) async {
    final shop = await pickShop(context);

    if (shop == null || !context.mounted) return;

    await recordVisit(context, ref, shop);
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final visits = ref.watch(visitsProvider);

    return Scaffold(
      appBar: const AppTopBar(title: 'Визиты', showBack: false),
      floatingActionButton: FloatingActionButton.extended(
        heroTag: null,
        backgroundColor: AppDecor.heroMid,
        foregroundColor: Colors.white,
        onPressed: () => _newVisit(context, ref),
        icon: const Icon(AppIcons.plus),
        label: const Text(
          'Визит',
          style: TextStyle(fontWeight: FontWeight.w800),
        ),
      ),
      body: visits.when(
        loading: () => const LoadingView(),
        error: (error, _) => ErrorView(
          message: errorText(error),
          onRetry: () => ref.invalidate(visitsProvider),
        ),
        data: (list) {
          if (list.isEmpty) {
            return const EmptyState(
              icon: AppIcons.location,
              title: 'Визитов пока нет',
              message: 'Отмечайте магазины, в которых вы побывали.',
            );
          }

          final children = <Widget>[];
          DateTime? lastDay;

          for (final visit in list) {
            final day = DateTime(
              visit.visitedAt.year,
              visit.visitedAt.month,
              visit.visitedAt.day,
            );

            if (lastDay != day) {
              lastDay = day;
              children.add(
                Padding(
                  padding: const EdgeInsets.only(top: 8, bottom: 10),
                  child: Text(
                    Formatters.date(day),
                    style: const TextStyle(
                      fontWeight: FontWeight.w800,
                      fontSize: 16,
                    ),
                  ),
                ),
              );
            }

            children.add(_VisitTile(visit: visit));
            children.add(const SizedBox(height: 10));
          }

          return RefreshIndicator(
            onRefresh: () async {
              ref.invalidate(visitsProvider);
              await ref.read(visitsProvider.future);
            },
            child: ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 96),
              children: children,
            ),
          );
        },
      ),
    );
  }
}

class _VisitTile extends StatelessWidget {
  const _VisitTile({required this.visit});

  final Visit visit;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    visit.shopName,
                    style: const TextStyle(
                      fontWeight: FontWeight.w800,
                      fontSize: 16,
                    ),
                  ),
                ),
                Text(
                  Formatters.time(visit.visitedAt),
                  style: const TextStyle(
                    color: AppColors.muted,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              visit.shopAddress,
              style: const TextStyle(
                color: AppColors.muted,
                fontWeight: FontWeight.w500,
              ),
            ),
            if (visit.note != null && visit.note!.isNotEmpty) ...[
              const SizedBox(height: 10),
              Text(visit.note!, style: const TextStyle(height: 1.35)),
            ],
          ],
        ),
      ),
    );
  }
}
