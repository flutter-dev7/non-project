import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/constants/app_icons.dart';
import '../../../../core/network/api_exception.dart';
import '../../../../core/router/app_routes.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/utils/app_snackbar.dart';
import '../../../../core/utils/submit_state_mixin.dart';
import '../../../../core/widgets/code_field.dart';
import '../../../../core/widgets/inline_message.dart';
import '../../../../core/widgets/primary_button.dart';
import '../../../../core/widgets/resend_timer_button.dart';
import '../providers/auth_providers.dart';
import '../widgets/auth_scaffold.dart';
import '../widgets/email_pill.dart';

class VerifyEmailPage extends ConsumerStatefulWidget {
  const VerifyEmailPage({super.key, required this.email, this.sendOnOpen = false});

  final String email;
  final bool sendOnOpen;

  @override
  ConsumerState<VerifyEmailPage> createState() => _VerifyEmailPageState();
}

class _VerifyEmailPageState extends ConsumerState<VerifyEmailPage>
    with SubmitStateMixin<VerifyEmailPage> {
  final _formKey = GlobalKey<FormState>();
  final _code = TextEditingController();

  @override
  void initState() {
    super.initState();

    if (widget.sendOnOpen) {
      WidgetsBinding.instance.addPostFrameCallback((_) => _resend(showMessage: false));
    }
  }

  @override
  void dispose() {
    _code.dispose();
    super.dispose();
  }

  Future<void> _verify() {
    return submit(
      _formKey,
      () => ref.read(authProvider.notifier).verifyEmail(widget.email, _code.text.trim()),
    );
  }

  Future<bool> _resend({bool showMessage = true}) async {
    try {
      await ref.read(resendVerificationUseCaseProvider)(widget.email);

      if (mounted && showMessage) {
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
      icon: AppIcons.email,
      title: 'Подтвердите email',
      subtitle: 'Мы отправили 6-значный код на адрес',
      onBack: () => context.go(AppRoutes.login),
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
        const SizedBox(height: 12),
        const Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(AppIcons.clock, size: 16, color: AppColors.muted),
            SizedBox(width: 6),
            Text(
              'Код действует 10 минут',
              style: TextStyle(color: AppColors.muted, fontWeight: FontWeight.w600),
            ),
          ],
        ),
        if (error != null) ...[
          const SizedBox(height: 16),
          InlineMessage(message: error!),
        ],
        const SizedBox(height: 24),
        PrimaryButton(label: 'Подтвердить', onPressed: _verify, isLoading: loading),
        const SizedBox(height: 12),
        Center(child: ResendTimerButton(onResend: _resend)),
        const SizedBox(height: 12),
        const InlineMessage(
          message: 'Не пришло письмо? Проверьте папку «Спам» или «Рассылки».',
          isError: false,
        ),
      ],
    );
  }
}