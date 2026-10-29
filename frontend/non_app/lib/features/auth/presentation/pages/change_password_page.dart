import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/constants/app_icons.dart';
import '../../../../core/utils/app_snackbar.dart';
import '../../../../core/utils/submit_state_mixin.dart';
import '../../../../core/utils/validators.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../../../../core/widgets/inline_message.dart';
import '../../../../core/widgets/primary_button.dart';
import '../providers/auth_providers.dart';
import '../widgets/auth_scaffold.dart';

class ChangePasswordPage extends ConsumerStatefulWidget {
  const ChangePasswordPage({super.key});

  @override
  ConsumerState<ChangePasswordPage> createState() => _ChangePasswordPageState();
}

class _ChangePasswordPageState extends ConsumerState<ChangePasswordPage>
    with SubmitStateMixin<ChangePasswordPage> {
  final _formKey = GlobalKey<FormState>();
  final _current = TextEditingController();
  final _password = TextEditingController();
  final _confirm = TextEditingController();

  @override
  void dispose() {
    _current.dispose();
    _password.dispose();
    _confirm.dispose();
    super.dispose();
  }

  Future<void> _submit() {
    return submit(_formKey, () async {
      await ref
          .read(authProvider.notifier)
          .changePassword(
            currentPassword: _current.text,
            newPassword: _password.text,
          );

      if (!mounted) return;

      AppSnackBar.show(
        context,
        'Пароль изменён. На других устройствах нужно войти заново.',
      );
      context.pop();
    });
  }

  @override
  Widget build(BuildContext context) {
    return AuthScaffold(
      icon: AppIcons.lock,
      title: 'Смена пароля',
      subtitle: 'После смены на других устройствах нужно будет войти заново',
      onBack: () => context.pop(),
      children: [
        Form(
          key: _formKey,
          child: Column(
            children: [
              AppTextField(
                controller: _current,
                label: 'Текущий пароль',
                hint: 'Введите текущий пароль',
                icon: AppIcons.lock,
                isPassword: true,
                textInputAction: TextInputAction.next,
                validator: Validators.requiredPassword,
                enabled: !loading,
              ),
              const SizedBox(height: 18),
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
                label: 'Повторите новый пароль',
                hint: 'Ещё раз новый пароль',
                icon: AppIcons.password,
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
        PrimaryButton(
          label: 'Сменить пароль',
          onPressed: _submit,
          isLoading: loading,
        ),
      ],
    );
  }
}
