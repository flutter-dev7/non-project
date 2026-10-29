import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../theme/app_colors.dart';
import '../theme/app_decor.dart';
import '../utils/validators.dart';

class CodeField extends StatefulWidget {
  const CodeField({
    super.key,
    required this.controller,
    this.onCompleted,
    this.enabled = true,
    this.autofocus = true,
    this.length = 6,
  });

  final TextEditingController controller;
  final ValueChanged<String>? onCompleted;
  final bool enabled;
  final bool autofocus;
  final int length;

  @override
  State<CodeField> createState() => _CodeFieldState();
}

class _CodeFieldState extends State<CodeField> {
  final FocusNode _focus = FocusNode();

  @override
  void dispose() {
    _focus.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return FormField<String>(
      validator: (_) => Validators.code(widget.controller.text),
      builder: (state) {
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            SizedBox(
              height: 62,
              child: Stack(
                children: [
                  Positioned.fill(
                    child: ListenableBuilder(
                      listenable: Listenable.merge([widget.controller, _focus]),
                      builder: (context, _) => _buildBoxes(state.hasError),
                    ),
                  ),
                  Positioned.fill(
                    child: Theme(
                      data: Theme.of(context).copyWith(
                        inputDecorationTheme: const InputDecorationTheme(),
                      ),
                      child: TextField(
                        controller: widget.controller,
                        focusNode: _focus,
                        enabled: widget.enabled,
                        autofocus: widget.autofocus,
                        keyboardType: TextInputType.number,
                        maxLength: widget.length,
                        inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                        autofillHints: const [AutofillHints.oneTimeCode],
                        showCursor: false,
                        enableInteractiveSelection: false,
                        cursorColor: Colors.transparent,
                        style: const TextStyle(color: Colors.transparent),
                        decoration: const InputDecoration(
                          border: InputBorder.none,
                          counterText: '',
                          isCollapsed: true,
                        ),
                        onChanged: (value) {
                          state.didChange(value);
                          if (state.hasError) state.validate();

                          if (value.length == widget.length) {
                            widget.onCompleted?.call(value);
                          }
                        },
                      ),
                    ),
                  ),
                ],
              ),
            ),
            if (state.hasError)
              Padding(
                padding: const EdgeInsets.only(top: 8, left: 4),
                child: Text(
                  state.errorText!,
                  style: const TextStyle(
                    color: AppColors.error,
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
          ],
        );
      },
    );
  }

  Widget _buildBoxes(bool hasError) {
    final text = widget.controller.text;
    final activeIndex = text.length >= widget.length ? widget.length - 1 : text.length;

    return IgnorePointer(
      child: Row(
        children: List.generate(widget.length, (i) {
          final filled = i < text.length;
          final active = _focus.hasFocus && i == activeIndex;

          final borderColor = hasError
              ? AppColors.error
              : active
                  ? AppDecor.heroMid
                  : filled
                      ? AppColors.primary.withValues(alpha: 0.4)
                      : Colors.transparent;

          return Expanded(
            child: Padding(
              padding: EdgeInsets.only(left: i == 0 ? 0 : 8),
              child: AnimatedContainer(
                duration: const Duration(milliseconds: 150),
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: filled ? Colors.white : AppDecor.fieldFill,
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: borderColor, width: 1.8),
                ),
                child: Text(
                  filled ? text[i] : '',
                  style: const TextStyle(
                    fontSize: 24,
                    fontWeight: FontWeight.w800,
                    color: AppColors.brown,
                  ),
                ),
              ),
            ),
          );
        }),
      ),
    );
  }
}