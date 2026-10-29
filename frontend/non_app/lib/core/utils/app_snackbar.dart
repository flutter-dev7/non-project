import 'package:flutter/material.dart';

import '../theme/app_colors.dart';

class AppSnackBar {
  AppSnackBar._();

  static void show(BuildContext context, String message, {bool isError = false}) {
    final messenger = ScaffoldMessenger.of(context);

    messenger
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Text(message),
          backgroundColor: isError ? AppColors.error : AppColors.brown,
        ),
      );
  }
}