import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../../../core/theme/app_decor.dart';
import '../../../../core/widgets/app_logo.dart';

class SplashPage extends StatelessWidget {
  const SplashPage({super.key});

  @override
  Widget build(BuildContext context) {
    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: SystemUiOverlayStyle.light.copyWith(statusBarColor: Colors.transparent),
      child: const Scaffold(
        body: DecoratedBox(
          decoration: BoxDecoration(gradient: AppDecor.heroGradient),
          child: Center(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                AppLogo(size: 120, showTagline: true, onDark: true),
                SizedBox(height: 40),
                SizedBox(
                  width: 28,
                  height: 28,
                  child: CircularProgressIndicator(strokeWidth: 3, color: Colors.white),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}