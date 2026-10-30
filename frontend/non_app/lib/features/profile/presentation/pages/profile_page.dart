import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:non_app/core/widgets/app_dialogs.dart';

import '../../../../core/constants/app_icons.dart';
import '../../../../core/network/api_exception.dart';
import '../../../../core/router/app_routes.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_decor.dart';
import '../../../../core/utils/app_snackbar.dart';
import '../../../../core/widgets/app_top_bar.dart';
import '../../../auth/domain/entities/auth_user.dart';
import '../../../auth/presentation/providers/auth_providers.dart';

class ProfilePage extends ConsumerWidget {
  const ProfilePage({super.key});

  Future<void> _logout(BuildContext context, WidgetRef ref) async {
    final confirmed = await AppDialogs.confirm(
      context,
      title: 'Выйти из аккаунта?',
      message: 'Чтобы снова заказывать хлеб, нужно будет войти заново.',
      confirmLabel: 'Выйти',
    );

    if (!confirmed) return;

    await ref.read(authProvider.notifier).logout();
  }

  Future<void> _logoutAll(BuildContext context, WidgetRef ref) async {
    final confirmed = await AppDialogs.confirm(
      context,
      title: 'Выйти на всех устройствах?',
      message: 'Все ваши сессии будут завершены, включая эту.',
      confirmLabel: 'Выйти везде',
      destructive: true,
    );

    if (!confirmed) return;

    try {
      await ref.read(authProvider.notifier).logoutAll();
    } on ApiException catch (e) {
      if (context.mounted) {
        AppSnackBar.show(context, e.message, isError: true);
      }
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(authProvider).valueOrNull;

    if (user == null) return const SizedBox.shrink();

    return Scaffold(
      appBar: const AppTopBar(title: 'Профиль', showBack: false),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
        children: [
          _HeaderCard(user: user),
          const SizedBox(height: 16),
          Card(
            child: Column(
              children: [
                _InfoTile(icon: AppIcons.phone, label: 'Телефон', value: user.phone),
                const Divider(indent: 64),
                _InfoTile(icon: AppIcons.email, label: 'Email', value: user.email ?? '—'),
              ],
            ),
          ),
          const SizedBox(height: 16),
          Card(
            child: Column(
              children: [
                _ActionTile(
                  icon: AppIcons.password,
                  label: 'Сменить пароль',
                  onTap: () => context.push(AppRoutes.changePassword),
                ),
                const Divider(indent: 64),
                _ActionTile(
                  icon: AppIcons.logout,
                  label: 'Выйти',
                  onTap: () => _logout(context, ref),
                ),
                const Divider(indent: 64),
                _ActionTile(
                  icon: AppIcons.devices,
                  label: 'Выйти на всех устройствах',
                  color: AppColors.error,
                  onTap: () => _logoutAll(context, ref),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _HeaderCard extends StatelessWidget {
  const _HeaderCard({required this.user});

  final AuthUser user;

  String get _initials {
    final first = user.firstName.isNotEmpty ? user.firstName[0] : '';
    final last = user.lastName.isNotEmpty ? user.lastName[0] : '';
    final text = (first + last).toUpperCase();

    return text.isEmpty ? '?' : text;
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: AppDecor.heroGradient,
        borderRadius: BorderRadius.circular(24),
      ),
      child: Row(
        children: [
          Container(
            width: 64,
            height: 64,
            alignment: Alignment.center,
            decoration: const BoxDecoration(
              color: AppColors.cream,
              shape: BoxShape.circle,
            ),
            child: Text(
              _initials,
              style: const TextStyle(
                fontSize: 22,
                fontWeight: FontWeight.w800,
                color: AppDecor.heroMid,
              ),
            ),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  user.fullName,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.w800,
                    color: Colors.white,
                  ),
                ),
                const SizedBox(height: 8),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 5),
                  decoration: BoxDecoration(
                    color: Colors.white.withValues(alpha: 0.18),
                    borderRadius: BorderRadius.circular(999),
                  ),
                  child: Text(
                    user.role.label,
                    style: const TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.w700,
                      fontSize: 13,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _InfoTile extends StatelessWidget {
  const _InfoTile({required this.icon, required this.label, required this.value});

  final IconData icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
      leading: Icon(icon, color: AppDecor.heroMid),
      title: Text(
        label,
        style: const TextStyle(
          fontSize: 12.5,
          color: AppColors.muted,
          fontWeight: FontWeight.w600,
        ),
      ),
      subtitle: Text(
        value,
        style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
      ),
    );
  }
}

class _ActionTile extends StatelessWidget {
  const _ActionTile({
    required this.icon,
    required this.label,
    required this.onTap,
    this.color,
  });

  final IconData icon;
  final String label;
  final VoidCallback onTap;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final tileColor = color ?? AppColors.brown;

    return ListTile(
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
      leading: Icon(icon, color: color ?? AppDecor.heroMid),
      title: Text(
        label,
        style: TextStyle(fontWeight: FontWeight.w700, color: tileColor),
      ),
      trailing: const Icon(AppIcons.forward, size: 18, color: AppColors.muted),
      onTap: onTap,
    );
  }
}