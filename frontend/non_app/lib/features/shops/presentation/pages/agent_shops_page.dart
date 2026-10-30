import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/constants/app_icons.dart';
import '../../../../core/router/app_routes.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_decor.dart';
import '../../../../core/utils/error_text.dart';
import '../../../../core/utils/formatters.dart';
import '../../../../core/utils/launcher.dart';
import '../../../../core/widgets/app_top_bar.dart';
import '../../../../core/widgets/empty_state.dart';
import '../../../../core/widgets/error_view.dart';
import '../../../../core/widgets/loading_view.dart';
import '../../domain/entities/shop.dart';
import '../providers/shops_providers.dart';

class AgentShopsPage extends ConsumerStatefulWidget {
  const AgentShopsPage({super.key});

  @override
  ConsumerState<AgentShopsPage> createState() => _AgentShopsPageState();
}

class _AgentShopsPageState extends ConsumerState<AgentShopsPage> {
  final _search = TextEditingController();
  Timer? _debounce;

  @override
  void dispose() {
    _debounce?.cancel();
    _search.dispose();
    super.dispose();
  }

  void _onChanged(String value) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 400), () {
      ref.read(shopSearchProvider.notifier).set(value);
    });
  }

  @override
  Widget build(BuildContext context) {
    final shops = ref.watch(shopsProvider);

    return Scaffold(
      appBar: AppTopBar(
        title: 'Магазины',
        showBack: false,
        actions: [
          AppIconButton(
            icon: AppIcons.plus,
            tooltip: 'Добавить магазин',
            onPressed: () => context.push(AppRoutes.shopForm),
          ),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 4, 20, 12),
            child: TextField(
              controller: _search,
              onChanged: _onChanged,
              decoration: const InputDecoration(
                hintText: 'Поиск по названию, адресу, телефону',
                prefixIcon: Icon(AppIcons.search, size: 22),
              ),
            ),
          ),
          Expanded(
            child: shops.when(
              loading: () => const LoadingView(),
              error: (error, _) => ErrorView(
                message: errorText(error),
                onRetry: () => ref.invalidate(shopsProvider),
              ),
              data: (page) => RefreshIndicator(
                onRefresh: () async {
                  ref.invalidate(shopsProvider);
                  await ref.read(shopsProvider.future);
                },
                child: page.items.isEmpty
                    ? ListView(
                        physics: const AlwaysScrollableScrollPhysics(),
                        children: const [
                          SizedBox(height: 60),
                          EmptyState(
                            icon: AppIcons.store,
                            title: 'Магазины не найдены',
                            message: 'Измените поиск или добавьте новый магазин.',
                          ),
                        ],
                      )
                    : ListView.separated(
                        physics: const AlwaysScrollableScrollPhysics(),
                        padding: const EdgeInsets.fromLTRB(20, 0, 20, 20),
                        itemCount: page.items.length + (page.hasMore ? 1 : 0),
                        separatorBuilder: (_, __) => const SizedBox(height: 12),
                        itemBuilder: (context, index) {
                          if (index == page.items.length) {
                            return const Padding(
                              padding: EdgeInsets.all(8),
                              child: Text(
                                'Показаны первые 100 магазинов. Уточните поиск.',
                                textAlign: TextAlign.center,
                                style: TextStyle(color: AppColors.muted),
                              ),
                            );
                          }

                          return _ShopTile(shop: page.items[index]);
                        },
                      ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _ShopTile extends StatelessWidget {
  const _ShopTile({required this.shop});

  final Shop shop;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(20),
        onTap: () => context.push(AppRoutes.shopDetails, extra: shop),
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
                      style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 17),
                    ),
                    const SizedBox(height: 6),
                    Text(
                      shop.address,
                      style: const TextStyle(
                        color: AppColors.muted,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    if (shop.debtLimit != null) ...[
                      const SizedBox(height: 4),
                      Text(
                        'Лимит долга: ${Formatters.money(shop.debtLimit!)}',
                        style: const TextStyle(
                          color: AppDecor.heroMid,
                          fontWeight: FontWeight.w700,
                          fontSize: 13,
                        ),
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
      ),
    );
  }
}