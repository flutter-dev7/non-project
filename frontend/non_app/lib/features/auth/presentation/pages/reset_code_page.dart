import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/constants/app_icons.dart';
import '../../../../core/network/api_exception.dart';
import '../../../../core/router/app_routes.dart';
import '../../../../core/utils/app_snackbar.dart';
import '../../../../core/utils/submit_state_mixin.dart';
import '../../../../core/widgets/code_field.dart';
import '../../../../core/widgets/inline_message.dart';
import '../../../../core/widgets/primary_button.dart';
import '../../../../core/widgets/resend_timer_button.dart';
import '../providers/auth_providers.dart';
import '../widgets/auth_scaffold.dart';
import '../widgets/email_pill.dart';

class ResetCodePage extends ConsumerStatefulWidget {
  const ResetCodePage({super.key, required this.email});

  final String email;

  @override
  ConsumerState<ResetCodePage> createState() => _ResetCodePageState();
}

class _ResetCodePageState extends ConsumerState<ResetCodePage>
    with SubmitStateMixin<ResetCodePage> {
  final _formKey = GlobalKey<FormState>();
  final _code = TextEditingController();

  @override
  void dispose() {
    _code.dispose();
    super.dispose();
  }

  Future<void> _verify() {
    return submit(_formKey, () async {
      final resetToken = await ref.read(verifyResetCodeUseCaseProvider)(
        email: widget.email,
        code: _code.text.trim(),
      );

      if (!mounted) return;
      context.go(AppRoutes.resetPassword, extra: resetToken);
    });
  }

  Future<bool> _resend() async {
    try {
      await ref.read(forgotPasswordUseCaseProvider)(widget.email);

      if (mounted) {
        AppSnackBar.show(context, 'Код отправлен на ${widget.email}');
      }

      return true;
    } on ApiException catch (e) {
      if (!mounted) return false;

      if (e.type == ApiErrorType.tooManyRequests) return true;

      setState(() => error = e.message);
      return false;
    }
  }

  @override
  Widget build(BuildContext context) {
    return AuthScaffold(
      icon: AppIcons.shield,
      title: 'Введите код',
      subtitle: 'Если аккаунт существует, мы отправили 6-значный код на адрес',
      onBack: () => context.go(AppRoutes.forgotPassword),
      children: [
        EmailPill(email: widget.email),
        const SizedBox(height: 24),
        Form(
          key: _formKey,
          child: CodeField(
            controller: _code,
            enabled: !loading,
            onCompleted: (_) => _verify(),
          ),
        ),
        if (error != null) ...[
          const SizedBox(height: 16),
          InlineMessage(message: error!),
        ],
        const SizedBox(height: 24),
        PrimaryButton(label: 'Продолжить', onPressed: _verify, isLoading: loading),
        const SizedBox(height: 12),
        Center(child: ResendTimerButton(onResend: _resend)),
      ],
    );
  }
}