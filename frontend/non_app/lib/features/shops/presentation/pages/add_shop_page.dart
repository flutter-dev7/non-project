import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/constants/app_icons.dart';
import '../../../../core/utils/app_snackbar.dart';
import '../../../../core/utils/submit_state_mixin.dart';
import '../../../../core/utils/validators.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../../../../core/widgets/app_top_bar.dart';
import '../../../../core/widgets/inline_message.dart';
import '../../../../core/widgets/primary_button.dart';
import '../providers/shops_providers.dart';

class AddShopPage extends ConsumerStatefulWidget {
  const AddShopPage({super.key});

  @override
  ConsumerState<AddShopPage> createState() => _AddShopPageState();
}

class _AddShopPageState extends ConsumerState<AddShopPage> with SubmitStateMixin<AddShopPage> {
  final _formKey = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _address = TextEditingController();
  final _phone = TextEditingController();
  final _contact = TextEditingController();

  @override
  void dispose() {
    _name.dispose();
    _address.dispose();
    _phone.dispose();
    _contact.dispose();
    super.dispose();
  }

  Future<void> _submit() {
    return submit(_formKey, () async {
      final contact = _contact.text.trim();

      await ref.read(createShopUseCaseProvider)(
        name: _name.text.trim(),
        address: _address.text.trim(),
        phone: Validators.toApiPhone(_phone.text),
        contactName: contact.isEmpty ? null : contact,
      );

      ref.invalidate(shopsProvider);

      if (!mounted) return;

      AppSnackBar.show(context, 'Магазин добавлен');
      context.pop();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: const AppTopBar(title: 'Новый магазин'),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Form(
                key: _formKey,
                child: Column(
                  children: [
                    AppTextField(
                      controller: _name,
                      label: 'Название магазина',
                      hint: 'Например, Сомон',
                      icon: AppIcons.store,
                      textInputAction: TextInputAction.next,
                      validator: Validators.name,
                      enabled: !loading,
                    ),
                    const SizedBox(height: 18),
                    AppTextField(
                      controller: _address,
                      label: 'Адрес',
                      hint: 'Улица, дом',
                      icon: AppIcons.location,
                      textInputAction: TextInputAction.next,
                      validator: Validators.required,
                      enabled: !loading,
                    ),
                    const SizedBox(height: 18),
                    AppTextField(
                      controller: _phone,
                      label: 'Телефон магазина',
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
                      controller: _contact,
                      label: 'Контактное лицо (необязательно)',
                      hint: 'Кого спросить',
                      icon: AppIcons.user,
                      textInputAction: TextInputAction.done,
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
              PrimaryButton(label: 'Добавить магазин', onPressed: _submit, isLoading: loading),
            ],
          ),
        ),
      ),
    );
  }
}