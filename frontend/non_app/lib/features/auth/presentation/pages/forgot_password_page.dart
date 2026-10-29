import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

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

class ForgotPasswordPage extends ConsumerStatefulWidget {
  const ForgotPasswordPage({super.key});

  @override
  ConsumerState<ForgotPasswordPage> createState() => _ForgotPasswordPageState();
}

class _ForgotPasswordPageState extends ConsumerState<ForgotPasswordPage>
    with SubmitStateMixin<ForgotPasswordPage> {
  final _formKey = GlobalKey<FormState>();
  final _email = TextEditingController();

  @override
  void dispose() {
    _email.dispose();
    super.dispose();
  }

  Future<void> _submit() {
    final email = _email.text.trim();

    return submit(
      _formKey,
      () async {
        await ref.read(forgotPasswordUseCaseProvider)(email);

        if (!mounted) return;
        context.go(AppRoutes.resetCodeFor(email));
      },
      onError: (e) {
        if (e.type != ApiErrorType.tooManyRequests) return false;

        context.go(AppRoutes.resetCodeFor(email));
        return true;
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return AuthScaffold(
      icon: AppIcons.password,
      title: 'Забыли пароль?',
      subtitle: 'Введите email, и мы отправим код для смены пароля',
      onBack: () => context.go(AppRoutes.login),
      children: [
        Form(
          key: _formKey,
          child: AppTextField(
            controller: _email,
            label: 'Электронная почта',
            hint: 'magazin@example.com',
            icon: AppIcons.email,
            keyboardType: TextInputType.emailAddress,
            textInputAction: TextInputAction.done,
            autofillHints: const [AutofillHints.email],
            autocorrect: false,
            validator: Validators.email,
            onSubmitted: (_) => _submit(),
            enabled: !loading,
          ),
        ),
        if (error != null) ...[
          const SizedBox(height: 16),
          InlineMessage(message: error!),
        ],
        const SizedBox(height: 24),
        PrimaryButton(label: 'Отправить код', onPressed: _submit, isLoading: loading),
      ],
    );
  }
}