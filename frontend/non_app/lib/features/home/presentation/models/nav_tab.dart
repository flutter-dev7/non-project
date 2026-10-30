import 'package:flutter/material.dart';
import 'package:flutter/widgets.dart';
import 'package:non_app/features/orders/presentation/pages/catalog_page.dart';
import 'package:non_app/features/orders/presentation/pages/orders_page.dart';
import 'package:phosphor_flutter/phosphor_flutter.dart';

import '../../../../core/widgets/empty_state.dart';
import '../../../auth/domain/entities/user_role.dart';
import '../../../profile/presentation/pages/profile_page.dart';

class NavTab {
  const NavTab({
    required this.label,
    required this.icon,
    required this.selectedIcon,
    required this.page,
  });

  final String label;
  final IconData icon;
  final IconData selectedIcon;
  final Widget page;
}

class _Soon extends StatelessWidget {
  const _Soon({required this.title, required this.icon, required this.message});

  final String title;
  final IconData icon;
  final String message;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(title)),
      body: EmptyState(icon: icon, title: 'Скоро', message: message),
    );
  }
}

const _profileTab = NavTab(
  label: 'Профиль',
  icon: PhosphorIconsRegular.userCircle,
  selectedIcon: PhosphorIconsFill.userCircle,
  page: ProfilePage(),
);

List<NavTab> tabsFor(UserRole role) {
  switch (role) {
    case UserRole.shop:
      return const [
        NavTab(
          label: 'Заказать',
          icon: PhosphorIconsRegular.shoppingCart,
          selectedIcon: PhosphorIconsFill.shoppingCart,
          page: CatalogPage(),
        ),
        NavTab(
          label: 'Заказы',
          icon: PhosphorIconsRegular.clipboardText,
          selectedIcon: PhosphorIconsFill.clipboardText,
          page: OrdersPage(),
        ),
        _profileTab,
      ];

    case UserRole.courier:
      return const [
        NavTab(
          label: 'Сегодня',
          icon: PhosphorIconsRegular.truck,
          selectedIcon: PhosphorIconsFill.truck,
          page: _Soon(
            title: 'Доставка сегодня',
            icon: PhosphorIconsBold.truck,
            message: 'Здесь будут заказы на сегодня',
          ),
        ),
        NavTab(
          label: 'История',
          icon: PhosphorIconsRegular.clockCounterClockwise,
          selectedIcon: PhosphorIconsFill.clockCounterClockwise,
          page: _Soon(
            title: 'История',
            icon: PhosphorIconsBold.clockCounterClockwise,
            message: 'Здесь будут выполненные доставки',
          ),
        ),
        _profileTab,
      ];

    case UserRole.agent:
      return const [
        NavTab(
          label: 'Магазины',
          icon: PhosphorIconsRegular.storefront,
          selectedIcon: PhosphorIconsFill.storefront,
          page: _Soon(
            title: 'Магазины',
            icon: PhosphorIconsBold.storefront,
            message: 'Здесь будут магазины и визиты',
          ),
        ),
        NavTab(
          label: 'Заказы',
          icon: PhosphorIconsRegular.clipboardText,
          selectedIcon: PhosphorIconsFill.clipboardText,
          page: _Soon(
            title: 'Заказы',
            icon: PhosphorIconsBold.clipboardText,
            message: 'Здесь вы будете записывать заказы магазинов',
          ),
        ),
        NavTab(
          label: 'Оплаты',
          icon: PhosphorIconsRegular.wallet,
          selectedIcon: PhosphorIconsFill.wallet,
          page: _Soon(
            title: 'Оплаты',
            icon: PhosphorIconsBold.wallet,
            message: 'Здесь вы будете записывать оплаты и смотреть долги',
          ),
        ),
        _profileTab,
      ];

    case UserRole.admin:
      return const [
        NavTab(
          label: 'Заказы',
          icon: PhosphorIconsRegular.clipboardText,
          selectedIcon: PhosphorIconsFill.clipboardText,
          page: _Soon(
            title: 'Все заказы',
            icon: PhosphorIconsBold.clipboardText,
            message: 'Здесь будут все заказы и их статусы',
          ),
        ),
        NavTab(
          label: 'План',
          icon: PhosphorIconsRegular.chartBar,
          selectedIcon: PhosphorIconsFill.chartBar,
          page: _Soon(
            title: 'План выпечки',
            icon: PhosphorIconsBold.chartBar,
            message: 'Здесь будет, сколько хлеба испечь на день',
          ),
        ),
        NavTab(
          label: 'Магазины',
          icon: PhosphorIconsRegular.storefront,
          selectedIcon: PhosphorIconsFill.storefront,
          page: _Soon(
            title: 'Магазины',
            icon: PhosphorIconsBold.storefront,
            message: 'Здесь будут магазины, долги и оплаты',
          ),
        ),
        _profileTab,
      ];
  }
}
