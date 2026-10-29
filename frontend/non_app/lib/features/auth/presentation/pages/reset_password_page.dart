import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/constants/app_icons.dart';
import '../../../../core/router/app_routes.dart';
import '../../../../core/utils/app_snackbar.dart';
import '../../../../core/utils/submit_state_mixin.dart';
import '../../../../core/utils/validators.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../../../../core/widgets/inline_message.dart';
import '../../../../core/widgets/primary_button.dart';
import '../providers/auth_providers.dart';
import '../widgets/auth_scaffold.dart';

class ResetPasswordPage extends ConsumerStatefulWidget {
  const ResetPasswordPage({super.key, required this.resetToken});

  final String resetToken;

  @override
  ConsumerState<ResetPasswordPage> createState() => _ResetPasswordPageState();
}

class _ResetPasswordPageState extends ConsumerState<ResetPasswordPage>
    with SubmitStateMixin<ResetPasswordPage> {
  final _formKey = GlobalKey<FormState>();
  final _password = TextEditingController();
  final _confirm = TextEditingController();

  @override
  void dispose() {
    _password.dispose();
    _confirm.dispose();
    super.dispose();
  }

  Future<void> _submit() {
    return submit(_formKey, () async {
      await ref.read(resetPasswordUseCaseProvider)(
        resetToken: widget.resetToken,
        newPassword: _password.text,
      );

      if (!mounted) return;

      AppSnackBar.show(context, 'Пароль изменён. Теперь войдите с новым паролем.');
      context.go(AppRoutes.login);
    });
  }

  @override
  Widget build(BuildContext context) {
    return AuthScaffold(
      icon: AppIcons.key,
      title: 'Новый пароль',
      subtitle: 'Придумайте пароль: не менее 8 символов, буквы и цифры',
      children: [
        Form(
          key: _formKey,
          child: Column(
            children: [
              AppTextField(
                controller: _password,
                label: 'Новый пароль',
                hint: 'Не менее 8 символов',
                icon: AppIcons.password,
                isPassword: true,
                textInputAction: TextInputAction.next,
                validator: Validators.password,
                enabled: !loading,
              ),
              const SizedBox(height: 18),
              AppTextField(
                controller: _confirm,
                label: 'Повторите пароль',
                hint: 'Ещё раз пароль',
                icon: AppIcons.lock,
                isPassword: true,
                textInputAction: TextInputAction.done,
                validator: Validators.confirmPassword(() => _password.text),
                onSubmitted: (_) => _submit(),
                enabled: !loading,
              ),
            ],
          ),
        ),
        if (error != null) ...[
          const SizedBox(height: 16),
          InlineMessage(message: error!),
        ],
        const SizedBox(height: 24),
        PrimaryButton(label: 'Сохранить пароль', onPressed: _submit, isLoading: loading),
        const SizedBox(height: 8),
        TextButton(
          onPressed: loading ? null : () => context.go(AppRoutes.forgotPassword),
          child: const Text('Начать заново'),
        ),
      ],
    );
  }
}