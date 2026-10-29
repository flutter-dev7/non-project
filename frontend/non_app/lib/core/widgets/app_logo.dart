import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import '../constants/app_assets.dart';
import '../theme/app_colors.dart';

class AppWordmark extends StatelessWidget {
  const AppWordmark({super.key, this.size = 40, this.color});

  final double size;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    return Text(
      'Нон',
      style: GoogleFonts.playfairDisplay(
        fontSize: size,
        fontWeight: FontWeight.w900,
        letterSpacing: -0.5,
        height: 1,
        color: color ?? AppColors.brown,
      ),
    );
  }
}

class AppLogo extends StatelessWidget {
  const AppLogo({
    super.key,
    this.size = 96,
    this.showName = true,
    this.showTagline = false,
    this.onDark = false,
  });

  final double size;
  final bool showName;
  final bool showTagline;
  final bool onDark;

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: size,
          height: size,
          padding: EdgeInsets.all(size * 0.16),
          decoration: BoxDecoration(
            color: AppColors.cream,
            shape: BoxShape.circle,
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: onDark ? 0.35 : 0.12),
                blurRadius: 24,
                offset: const Offset(0, 10),
              ),
            ],
          ),
          child: Image.asset(AppAssets.logo),
        ),
        if (showName) ...[
          const SizedBox(height: 16),
          AppWordmark(size: size * 0.5, color: onDark ? AppColors.cream : AppColors.brown),
        ],
        if (showTagline) ...[
          const SizedBox(height: 8),
          Text(
            'ОПТОВЫЕ ПОСТАВКИ ХЛЕБА',
            style: TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.w700,
              letterSpacing: 2.2,
              color: onDark
                  ? AppColors.cream.withValues(alpha: 0.75)
                  : AppColors.muted,
            ),
          ),
        ],
      ],
    );
  }
}