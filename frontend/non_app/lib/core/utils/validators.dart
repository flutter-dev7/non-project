import 'package:flutter/material.dart';

class Validators {
  Validators._();

  static final RegExp _emailRegex = RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$');
  static final RegExp _letter = RegExp(r'\p{L}', unicode: true);
  static final RegExp _digit = RegExp(r'\d');

  static String? required(String? value) =>
      (value == null || value.trim().isEmpty) ? 'Обязательное поле' : null;

  static String? name(String? value) {
    final text = value?.trim() ?? '';

    if (text.isEmpty) return 'Обязательное поле';
    if (text.length < 2) return 'Минимум 2 символа';

    return null;
  }

  static String? email(String? value) {
    final text = value?.trim() ?? '';

    if (text.isEmpty) return 'Введите email';
    if (!_emailRegex.hasMatch(text)) return 'Неверный формат email';

    return null;
  }

  static String phoneDigits(String? value) {
    var digits = (value ?? '').replaceAll(RegExp(r'\D'), '');

    if (digits.length == 12 && digits.startsWith('992')) {
      digits = digits.substring(3);
    } else if (digits.length == 10 && digits.startsWith('0')) {
      digits = digits.substring(1);
    }

    return digits;
  }

  static String toApiPhone(String? value) => '+992${phoneDigits(value)}';

  static String? phone(String? value) {
    final digits = phoneDigits(value);

    if (digits.isEmpty) return 'Введите номер телефона';
    if (digits.length != 9) return 'Номер должен содержать 9 цифр';

    return null;
  }

  static String? password(String? value) {
    final text = value ?? '';

    if (text.length < 8) return 'Минимум 8 символов';
    if (text.length > 100) return 'Не более 100 символов';
    if (!_letter.hasMatch(text) || !_digit.hasMatch(text)) {
      return 'Пароль должен содержать буквы и цифры';
    }

    return null;
  }

  static String? requiredPassword(String? value) =>
      (value == null || value.isEmpty) ? 'Введите пароль' : null;

  static String? code(String? value) {
    final text = value?.trim() ?? '';

    if (text.length != 6) return 'Введите 6 цифр';

    return null;
  }

    static String? Function(String?) confirmPassword(String Function() original) {
    return (value) => value != original() ? 'Пароли не совпадают' : null;
  }
}

abstract class TextEditingControllerLike {
  String get text;
}


extension TextEditingControllerAsLike on TextEditingController {
  TextEditingControllerLike get asLike => _ControllerLike(this);
}

class _ControllerLike implements TextEditingControllerLike {
  _ControllerLike(this._controller);

  final TextEditingController _controller;

  @override
  String get text => _controller.text;
}