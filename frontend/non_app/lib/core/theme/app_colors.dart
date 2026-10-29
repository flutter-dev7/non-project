import 'package:flutter/material.dart';

class AppColors {
  AppColors._();

  static const Color primary = Color(0xFFC8801A);
  static const Color brown = Color(0xFF3B2A1A);
  static const Color cream = Color(0xFFFFF8EE);
  static const Color border = Color(0xFFE8DCC8);
  static const Color muted = Color(0xFF8A7A68);
  static const Color error = Color(0xFFC0392B);
  static const Color success = Color(0xFF2E8B57);

  static const Color statusNew = Color(0xFF3B82F6);
  static const Color statusAccepted = Color(0xFFF59E0B);
  static const Color statusBaking = Color(0xFFEA580C);
  static const Color statusOnTheWay = Color(0xFF8B5CF6);
  static const Color statusDelivered = Color(0xFF16A34A);
  static const Color statusCancelled = Color(0xFF9CA3AF);

  static Color forStatus(String status) => switch (status) {
        'New' => statusNew,
        'Accepted' => statusAccepted,
        'Baking' => statusBaking,
        'OnTheWay' => statusOnTheWay,
        'Delivered' => statusDelivered,
        'Cancelled' => statusCancelled,
        _ => muted,
      };
}