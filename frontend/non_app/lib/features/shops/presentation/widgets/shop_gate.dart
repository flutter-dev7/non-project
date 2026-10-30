import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/utils/error_text.dart';
import '../../../../core/widgets/app_top_bar.dart';
import '../../../../core/widgets/error_view.dart';
import '../../../../core/widgets/loading_view.dart';
import '../providers/shops_providers.dart';
import 'create_shop_view.dart';

class ShopGate extends ConsumerWidget {
  const ShopGate({super.key, required this.title, required this.child});

  final String title;
  final Widget child;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final shop = ref.watch(myShopProvider);

    return shop.when(
      loading: () => Scaffold(
        appBar: AppTopBar(title: title, showBack: false),
        body: const LoadingView(),
      ),
      error: (error, _) => Scaffold(
        appBar: AppTopBar(title: title, showBack: false),
        body: ErrorView(
          message: errorText(error),
          onRetry: () => ref.invalidate(myShopProvider),
        ),
      ),
      data: (value) => value == null ? const CreateShopView() : child,
    );
  }
}