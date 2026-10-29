import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
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

class RegisterPage extends ConsumerStatefulWidget {
  const RegisterPage({super.key});

  @override
  ConsumerState<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends ConsumerState<RegisterPage>
    with SubmitStateMixin<RegisterPage> {
  final _formKey = GlobalKey<FormState>();
  final _firstName = TextEditingController();
  final _lastName = TextEditingController();
  final _phone = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();
  final _confirm = TextEditingController();

  @override
  void dispose() {
    _firstName.dispose();
    _lastName.dispose();
    _phone.dispose();
    _email.dispose();
    _password.dispose();
    _confirm.dispose();
    super.dispose();
  }

  Future<void> _submit() {
    final email = _email.text.trim();

    return submit(_formKey, () async {
      await ref.read(registerUseCaseProvider)(
        firstName: _firstName.text.trim(),
        lastName: _lastName.text.trim(),
        phone: Validators.toApiPhone(_phone.text),
        email: email,
        password: _password.text,
      );

      if (!mounted) return;

      AppSnackBar.show(context, 'Мы отправили код на $email');
      context.go(AppRoutes.verifyEmailFor(email));
    });
  }

  @override
  Widget build(BuildContext context) {
    return AuthScaffold(
      icon: AppIcons.store,
      title: 'Регистрация магазина',
      subtitle: 'Создайте аккаунт для ежедневного заказа хлеба',
      onBack: () => context.go(AppRoutes.login),
      children: [
        Form(
          key: _formKey,
          child: Column(
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: AppTextField(
                      controller: _firstName,
                      label: 'Имя',
                      hint: 'Рустам',
                      icon: AppIcons.user,
                      textInputAction: TextInputAction.next,
                      validator: Validators.name,
                      enabled: !loading,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: AppTextField(
                      controller: _lastName,
                      label: 'Фамилия',
                      hint: 'Каримов',
                      textInputAction: TextInputAction.next,
                      validator: Validators.name,
                      enabled: !loading,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 18),
              AppTextField(
                controller: _phone,
                label: 'Номер телефона',
                hint: '900 00 00 00',
                icon: AppIcons.phone,
                prefixText: '+992 ',
                keyboardType: TextInputType.phone,
                textInputAction: TextInputAction.next,
                inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9 ]'))],
                validator: Validators.phone,
                enabled: !loading,
              ),
              const SizedBox(height: 18),
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
        const SizedBox(height: 14),
        const InlineMessage(
          message: 'Пароль: не менее 8 символов, буквы и цифры. '
              'На почту придёт код подтверждения.',
          isError: false,
        ),
        if (error != null) ...[
          const SizedBox(height: 14),
          InlineMessage(message: error!),
        ],
        const SizedBox(height: 24),
        PrimaryButton(label: 'Зарегистрироваться', onPressed: _submit, isLoading: loading),
        const SizedBox(height: 16),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Text('Уже есть аккаунт?', style: TextStyle(fontWeight: FontWeight.w500)),
            TextButton(
              onPressed: loading ? null : () => context.go(AppRoutes.login),
              child: const Text('Войти'),
            ),
          ],
        ),
      ],
    );
  }
}