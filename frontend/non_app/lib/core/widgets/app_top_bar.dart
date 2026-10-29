import 'package:flutter/material.dart';

import '../constants/app_icons.dart';
import '../theme/app_colors.dart';
import '../theme/app_decor.dart';

class AppIconButton extends StatelessWidget {
  const AppIconButton({
    super.key,
    required this.icon,
    required this.onPressed,
    this.tooltip,
    this.onDark = false,
  });

  final IconData icon;
  final VoidCallback? onPressed;
  final String? tooltip;
  final bool onDark;

  @override
  Widget build(BuildContext context) {
    final button = Material(
      color: onDark ? Colors.white.withValues(alpha: 0.16) : AppDecor.fieldFill,
      borderRadius: BorderRadius.circular(14),
      child: InkWell(
        onTap: onPressed,
        borderRadius: BorderRadius.circular(14),
        child: SizedBox(
          width: 44,
          height: 44,
          child: Icon(icon, size: 22, color: onDark ? Colors.white : AppColors.brown),
        ),
      ),
    );

    return tooltip == null ? button : Tooltip(message: tooltip!, child: button);
  }
}

class AppBackButton extends StatelessWidget {
  const AppBackButton({super.key, this.onPressed, this.onDark = false});

  final VoidCallback? onPressed;
  final bool onDark;

  @override
  Widget build(BuildContext context) {
    return AppIconButton(
      icon: AppIcons.back,
      tooltip: 'Назад',
      onDark: onDark,
      onPressed: onPressed ?? () => Navigator.of(context).maybePop(),
    );
  }
}

class AppTopBar extends StatelessWidget implements PreferredSizeWidget {
  const AppTopBar({
    super.key,
    this.title,
    this.titleWidget,
    this.actions = const [],
    this.showBack = true,
  });

  final String? title;
  final Widget? titleWidget;
  final List<Widget> actions;
  final bool showBack;

  @override
  Size get preferredSize => const Size.fromHeight(64);

  @override
  Widget build(BuildContext context) {
    final canPop = Navigator.of(context).canPop();

    return AppBar(
      automaticallyImplyLeading: false,
      leading: showBack && canPop
          ? const Padding(
              padding: EdgeInsets.only(left: 16),
              child: Align(alignment: Alignment.centerLeft, child: AppBackButton()),
            )
          : null,
      title: titleWidget ?? (title == null ? null : Text(title!)),
      actions: [
        for (final action in actions)
          Padding(padding: const EdgeInsets.only(right: 8), child: action),
        const SizedBox(width: 8),
      ],
    );
  }
}