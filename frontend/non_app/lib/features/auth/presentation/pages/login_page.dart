import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/constants/api_messages.dart';
import '../../../../core/constants/app_icons.dart';
import '../../../../core/network/api_exception.dart';
import '../../../../core/router/app_routes.dart';
import '../../../../core/utils/submit_state_mixin.dart';
import '../../../../core/utils/validators.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../../../../core/widgets/inline_message.dart';
import '../../../../core/widgets/primary_button.dart';
import '../providers/auth_providers.dart';
import '../widgets/auth_scaffold.dart';

class LoginPage extends ConsumerStatefulWidget {
  const LoginPage({super.key});

  @override
  ConsumerState<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends ConsumerState<LoginPage> with SubmitStateMixin<LoginPage> {
  final _formKey = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _password = TextEditingController();

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() {
    final email = _email.text.trim();

    return submit(
      _formKey,
      () => ref.read(authProvider.notifier).login(email, _password.text),
      onError: (e) {
        final notConfirmed = e.type == ApiErrorType.forbidden &&
            e.message.startsWith(ApiMessages.emailNotConfirmed);

        if (notConfirmed) {
          context.go(AppRoutes.verifyEmailFor(email, send: true));
        }

        return notConfirmed;
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return AuthScaffold(
      big: true,
      title: 'С возвращением',
      subtitle: 'Войдите, чтобы заказывать свежий хлеб',
      children: [
        Form(
          key: _formKey,
          child: Column(
            children: [
              AppTextField(
                controller: _email,
                label: 'Электронная почта',
                hint: 'magazin@example.com',
                icon: AppIcons.email,
                keyboardType: TextInputType.emailAddress,
                textInputAction: TextInputAction.next,
                autofillHints: const [AutofillHints.email],
                autocorrect: false,
                validator: Validators.email,
                enabled: !loading,
              ),
              const SizedBox(height: 18),
              AppTextField(
                controller: _password,
                label: 'Пароль',
                hint: 'Введите пароль',
                icon: AppIcons.password,
                isPassword: true,
                textInputAction: TextInputAction.done,
                autofillHints: const [AutofillHints.password],
                validator: Validators.requiredPassword,
                onSubmitted: (_) => _submit(),
                enabled: !loading,
              ),
            ],
          ),
        ),
        Align(
          alignment: Alignment.centerRight,
          child: TextButton(
            onPressed: loading ? null : () => context.go(AppRoutes.forgotPassword),
            child: const Text('Забыли пароль?'),
          ),
        ),
        if (error != null) ...[
          InlineMessage(message: error!),
          const SizedBox(height: 16),
        ],
        PrimaryButton(label: 'Войти', onPressed: _submit, isLoading: loading),
        const SizedBox(height: 20),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Text('Ещё нет аккаунта?', style: TextStyle(fontWeight: FontWeight.w500)),
            TextButton(
              onPressed: loading ? null : () => context.go(AppRoutes.register),
              child: const Text('Зарегистрироваться'),
            ),
          ],
        ),
      ],
    );
  }
}