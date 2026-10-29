import 'package:flutter/widgets.dart';

import '../network/api_exception.dart';

mixin SubmitStateMixin<T extends StatefulWidget> on State<T> {
  bool loading = false;
  String? error;

  /// Валидирует форму, показывает загрузку, выполняет [action].
  /// [onError] вернёт true, если ошибку обработали сами (тогда текст не показываем).
  Future<void> submit(
    GlobalKey<FormState>? formKey,
    Future<void> Function() action, {
    bool Function(ApiException error)? onError,
  }) async {
    if (loading) return;
    if (formKey != null && !formKey.currentState!.validate()) return;

    FocusScope.of(context).unfocus();
    setState(() {
      loading = true;
      error = null;
    });

    try {
      await action();
    } on ApiException catch (e) {
      if (!mounted) return;

      if (onError?.call(e) ?? false) return;

      setState(() => error = e.message);
    } finally {
      if (mounted) {
        setState(() => loading = false);
      }
    }
  }
}