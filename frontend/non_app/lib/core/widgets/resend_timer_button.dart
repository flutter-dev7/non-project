import 'dart:async';

import 'package:flutter/material.dart';

import '../constants/app_icons.dart';
import '../theme/app_colors.dart';
import '../theme/app_decor.dart';

class ResendTimerButton extends StatefulWidget {
  const ResendTimerButton({
    super.key,
    required this.onResend,
    this.seconds = 60,
    this.label = 'Отправить код ещё раз',
  });

  final Future<bool> Function() onResend;
  final int seconds;
  final String label;

  @override
  State<ResendTimerButton> createState() => _ResendTimerButtonState();
}

class _ResendTimerButtonState extends State<ResendTimerButton> {
  Timer? _timer;
  int _left = 0;
  bool _sending = false;

  @override
  void initState() {
    super.initState();
    _beginCountdown();
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  void _beginCountdown() {
    _timer?.cancel();
    _left = widget.seconds;

    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (!mounted) {
        timer.cancel();
        return;
      }

      setState(() {
        _left--;

        if (_left <= 0) {
          _left = 0;
          timer.cancel();
        }
      });
    });
  }

  Future<void> _resend() async {
    setState(() => _sending = true);

    final sent = await widget.onResend();

    if (!mounted) return;

    setState(() {
      _sending = false;

      if (sent) {
        _beginCountdown();
      }
    });
  }

  String get _timeText {
    final minutes = _left ~/ 60;
    final seconds = (_left % 60).toString().padLeft(2, '0');

    return '$minutes:$seconds';
  }

  @override
  Widget build(BuildContext context) {
    if (_left > 0) {
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: BoxDecoration(
          color: AppDecor.fieldFill,
          borderRadius: BorderRadius.circular(14),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(AppIcons.clock, size: 20, color: AppColors.muted),
            const SizedBox(width: 8),
            const Text(
              'Повторный код через',
              style: TextStyle(color: AppColors.muted, fontWeight: FontWeight.w600),
            ),
            const SizedBox(width: 8),
            Text(_timeText, style: const TextStyle(fontWeight: FontWeight.w800)),
          ],
        ),
      );
    }

    return TextButton.icon(
      onPressed: _sending ? null : _resend,
      icon: const Icon(AppIcons.refresh, size: 18),
      label: Text(widget.label),
    );
  }
}