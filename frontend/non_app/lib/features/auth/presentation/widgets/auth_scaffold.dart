import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_decor.dart';
import '../../../../core/widgets/app_logo.dart';
import '../../../../core/widgets/app_top_bar.dart';

/// Тёмный градиентный верх + белый «лист» с формой, выезжающий снизу.
/// [big] — главный экран входа (логотип и название), иначе компактный
/// верх с иконкой [icon] и кнопкой «назад», если задан [onBack].
class AuthScaffold extends StatelessWidget {
  const AuthScaffold({
    super.key,
    required this.title,
    required this.children,
    this.subtitle,
    this.icon,
    this.big = false,
    this.onBack,
  });

  final String title;
  final String? subtitle;
  final List<Widget> children;
  final IconData? icon;
  final bool big;
  final VoidCallback? onBack;

  @override
  Widget build(BuildContext context) {
    final bottomInset = MediaQuery.of(context).padding.bottom;

    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: SystemUiOverlayStyle.light.copyWith(statusBarColor: Colors.transparent),
      child: Scaffold(
        backgroundColor: AppDecor.heroDark,
        body: DecoratedBox(
          decoration: const BoxDecoration(gradient: AppDecor.heroGradient),
          child: CustomScrollView(
            physics: const ClampingScrollPhysics(),
            slivers: [
              SliverFillRemaining(
                hasScrollBody: false,
                child: Column(
                  children: [
                    _Hero(big: big, icon: icon, onBack: onBack),
                    Expanded(
                      child: Container(
                        width: double.infinity,
                        padding: EdgeInsets.fromLTRB(24, 32, 24, 24 + bottomInset),
                        decoration: const BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.vertical(top: Radius.circular(32)),
                        ),
                        child: Align(
                          alignment: Alignment.topCenter,
                          child: ConstrainedBox(
                            constraints: const BoxConstraints(maxWidth: 440),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.stretch,
                              children: [
                                Text(
                                  title,
                                  style: const TextStyle(
                                    fontSize: 26,
                                    fontWeight: FontWeight.w800,
                                    height: 1.15,
                                  ),
                                ),
                                if (subtitle != null) ...[
                                  const SizedBox(height: 8),
                                  Text(
                                    subtitle!,
                                    style: const TextStyle(
                                      color: AppColors.muted,
                                      fontWeight: FontWeight.w500,
                                      height: 1.4,
                                    ),
                                  ),
                                ],
                                const SizedBox(height: 28),
                                ...children,
                              ],
                            ),
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _Hero extends StatelessWidget {
  const _Hero({required this.big, required this.icon, required this.onBack});

  final bool big;
  final IconData? icon;
  final VoidCallback? onBack;

  @override
  Widget build(BuildContext context) {
    final top = MediaQuery.of(context).padding.top;

    return SizedBox(
      width: double.infinity,
      child: Stack(
        children: [
          Positioned(
            top: -50,
            right: -40,
            child: _Circle(size: 190, color: Colors.white.withValues(alpha: 0.06)),
          ),
          Positioned(
            bottom: -30,
            left: -50,
            child: _Circle(size: 140, color: AppColors.primary.withValues(alpha: 0.22)),
          ),
          Padding(
            padding: EdgeInsets.fromLTRB(20, top + 16, 20, big ? 44 : 40),
            child: big ? _bigContent() : _compactContent(),
          ),
        ],
      ),
    );
  }

  Widget _bigContent() {
    return const Center(
      child: AppLogo(size: 92, showTagline: true, onDark: true),
    );
  }

  Widget _compactContent() {
    return Column(
      children: [
        SizedBox(
          height: 44,
          child: Align(
            alignment: Alignment.centerLeft,
            child: onBack == null ? null : AppBackButton(onPressed: onBack, onDark: true),
          ),
        ),
        const SizedBox(height: 12),
        Container(
          width: 76,
          height: 76,
          decoration: BoxDecoration(
            color: AppColors.cream,
            shape: BoxShape.circle,
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.3),
                blurRadius: 22,
                offset: const Offset(0, 10),
              ),
            ],
          ),
          child: Icon(icon, size: 34, color: AppDecor.heroMid),
        ),
      ],
    );
  }
}

class _Circle extends StatelessWidget {
  const _Circle({required this.size, required this.color});

  final double size;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(shape: BoxShape.circle, color: color),
    );
  }
}