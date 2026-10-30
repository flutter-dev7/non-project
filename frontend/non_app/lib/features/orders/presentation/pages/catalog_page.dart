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
import '../../../../core/widgets/quantity_stepper.dart';
import '../../../products/domain/entities/product.dart';
import '../../../products/presentation/providers/products_providers.dart';
import '../../../shops/presentation/widgets/shop_gate.dart';
import '../../domain/entities/order_draft.dart';
import '../providers/orders_providers.dart';

class CatalogPage extends StatelessWidget {
  const CatalogPage({super.key});

  @override
  Widget build(BuildContext context) {
    return const ShopGate(title: 'Хлеб', child: _CatalogView());
  }
}

class _CatalogView extends ConsumerWidget {
  const _CatalogView();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final products = ref.watch(productsProvider);
    final cart = ref.watch(cartProvider);

    return Scaffold(
      appBar: const AppTopBar(title: 'Хлеб', showBack: false),
      body: products.when(
        loading: () => const LoadingView(),
        error: (error, _) => ErrorView(
          message: errorText(error),
          onRetry: () => ref.invalidate(productsProvider),
        ),
        data: (list) {
          if (list.isEmpty) {
            return const EmptyState(
              icon: AppIcons.store,
              title: 'Пока нет товаров',
              message: 'Каталог пуст. Загляните позже.',
            );
          }

          return RefreshIndicator(
            onRefresh: () async {
              ref.invalidate(productsProvider);
              await ref.read(productsProvider.future);
            },
            child: ListView.separated(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 20),
              itemCount: list.length,
              separatorBuilder: (_, __) => const SizedBox(height: 12),
              itemBuilder: (context, index) => _ProductTile(product: list[index]),
            ),
          );
        },
      ),
      bottomNavigationBar: cart.isEmpty ? null : _CartBar(cart: cart),
    );
  }
}

class _ProductTile extends ConsumerWidget {
  const _ProductTile({required this.product});

  final Product product;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final quantity = ref.watch(cartProvider.select((c) => c[product.id]?.quantity ?? 0));

    void change(int value) {
      ref.read(cartProvider.notifier).setLine(
            OrderLine(
              productId: product.id,
              name: product.name,
              unitPrice: product.price,
              quantity: value,
            ),
          );
    }

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          children: [
            Row(
              children: [
                _ProductImage(url: product.imageUrl),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        product.name,
                        style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 16),
                      ),
                      if (product.weightInGrams != null) ...[
                        const SizedBox(height: 4),
                        Text(
                          '${product.weightInGrams} г',
                          style: const TextStyle(
                            color: AppColors.muted,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Text(
                  Formatters.money(product.price),
                  style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 17),
                ),
                const Spacer(),
                if (quantity == 0)
                  Material(
                    color: AppDecor.heroMid,
                    borderRadius: BorderRadius.circular(14),
                    child: InkWell(
                      borderRadius: BorderRadius.circular(14),
                      onTap: () => change(1),
                      child: const SizedBox(
                        width: 112,
                        height: 36,
                        child: Icon(AppIcons.plus, size: 18, color: Colors.white),
                      ),
                    ),
                  )
                else
                  QuantityStepper(value: quantity, onChanged: change),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _ProductImage extends StatelessWidget {
  const _ProductImage({this.url});

  final String? url;

  @override
  Widget build(BuildContext context) {
    final placeholder = Container(
      width: 64,
      height: 64,
      decoration: BoxDecoration(
        color: AppDecor.fieldFill,
        borderRadius: BorderRadius.circular(16),
      ),
      child: const Icon(Icons.bakery_dining_outlined, color: AppDecor.heroMid, size: 30),
    );

    if (url == null || url!.isEmpty) return placeholder;

    return ClipRRect(
      borderRadius: BorderRadius.circular(16),
      child: Image.network(
        url!,
        width: 64,
        height: 64,
        fit: BoxFit.cover,
        errorBuilder: (_, __, ___) => placeholder,
      ),
    );
  }
}

class _CartBar extends StatelessWidget {
  const _CartBar({required this.cart});

  final Map<String, OrderLine> cart;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.fromLTRB(20, 12, 20, 12),
      color: AppColors.cream,
      child: Material(
        color: AppDecor.heroMid,
        borderRadius: BorderRadius.circular(18),
        child: InkWell(
          borderRadius: BorderRadius.circular(18),
          onTap: () => context.push(AppRoutes.orderForm),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 16),
            child: Row(
              children: [
                const Icon(AppIcons.cart, color: Colors.white),
                const SizedBox(width: 12),
                Text(
                  '${cart.totalQuantity} шт.',
                  style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w700),
                ),
                const Spacer(),
                Text(
                  Formatters.money(cart.totalAmount),
                  style: const TextStyle(
                    color: Colors.white,
                    fontWeight: FontWeight.w800,
                    fontSize: 16,
                  ),
                ),
                const SizedBox(width: 12),
                const Icon(AppIcons.forward, color: Colors.white, size: 20),
              ],
            ),
          ),
        ),
      ),
    );
  }
}