import 'package:flutter/material.dart';

class AppDecor {
  AppDecor._();

  static const Color heroDark = Color(0xFF2B1D10);
  static const Color heroMid = Color(0xFF6E4110);
  static const Color heroLight = Color(0xFFB06F14);
  static const Color fieldFill = Color(0xFFFFF3E2);

  static const LinearGradient heroGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [heroDark, heroMid, heroLight],
  );
}