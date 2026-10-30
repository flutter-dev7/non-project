import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:non_app/core/widgets/app_dialogs.dart';

import '../../../../core/constants/app_icons.dart';
import '../../../../core/network/api_exception.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_decor.dart';
import '../../../../core/utils/app_snackbar.dart';
import '../../../../core/utils/formatters.dart';
import '../../../../core/widgets/primary_button.dart';
import '../../domain/entities/order.dart';
import '../../domain/entities/order_status.dart';
import '../../domain/entities/payment_method.dart';
import '../../domain/entities/payment_type.dart';
import '../providers/courier_providers.dart';
import '../providers/orders_providers.dart';

OrderStatus? nextCourierStatus(Order order) {
  switch (order.status) {
    case OrderStatus.baking:
      return OrderStatus.onTheWay;
    case OrderStatus.onTheWay:
      return OrderStatus.delivered;
    default:
      return null;
  }
}

class _PaymentInput {
  const _PaymentInput(this.amount, this.method);

  final double amount;
  final PaymentMethod method;
}

class CourierActionButton extends ConsumerWidget {
  const CourierActionButton({super.key, required this.order});

  final Order order;

  Future<void> _advance(BuildContext context, WidgetRef ref, OrderStatus next) async {
    try {
      if (next == OrderStatus.delivered && order.paymentType == PaymentType.now) {
        final payment = await showModalBottomSheet<_PaymentInput>(
          context: context,
          isScrollControlled: true,
          showDragHandle: true,
          backgroundColor: Colors.white,
          builder: (_) => _PaymentSheet(order: order),
        );

        if (payment == null) return;

        await ref.read(changeOrderStatusUseCaseProvider)(
          order.id,
          next,
          paymentAmount: payment.amount,
          paymentMethod: payment.method,
        );
      } else {
        final confirmed = await AppDialogs.confirm(
          context,
          title: next == OrderStatus.delivered ? 'Заказ доставлен?' : 'Забрали заказ?',
          message: order.shopName,
          confirmLabel: next == OrderStatus.delivered ? 'Доставлено' : 'Забрал',
        );

        if (!confirmed) return;

        await ref.read(changeOrderStatusUseCaseProvider)(order.id, next);
      }

      ref.invalidate(courierTodayProvider);
      ref.invalidate(courierHistoryProvider);
      ref.invalidate(orderDetailsProvider(order.id));
      ref.invalidate(ordersProvider);

      if (context.mounted) {
        AppSnackBar.show(context, 'Статус: ${next.label}');
      }
    } on ApiException catch (e) {
      if (context.mounted) {
        AppSnackBar.show(context, e.message, isError: true);
      }
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final next = nextCourierStatus(order);

    if (next == null) return const SizedBox.shrink();

    final delivering = next == OrderStatus.delivered;

    return PrimaryButton(
      label: delivering ? 'Доставлено' : 'Забрал заказ',
      icon: delivering ? AppIcons.success : AppIcons.truck,
      onPressed: () => _advance(context, ref, next),
    );
  }
}

class _PaymentSheet extends StatefulWidget {
  const _PaymentSheet({required this.order});

  final Order order;

  @override
  State<_PaymentSheet> createState() => _PaymentSheetState();
}

class _PaymentSheetState extends State<_PaymentSheet> {
  late final TextEditingController _amount;
  PaymentMethod _method = PaymentMethod.cash;
  String? _error;

  @override
  void initState() {
    super.initState();

    final total = widget.order.totalAmount;
    _amount = TextEditingController(
      text: total == total.roundToDouble() ? total.toStringAsFixed(0) : total.toString(),
    );
  }

  @override
  void dispose() {
    _amount.dispose();
    super.dispose();
  }

  void _submit() {
    final value = double.tryParse(_amount.text.trim().replaceAll(',', '.'));

    if (value == null || value <= 0) {
      setState(() => _error = 'Введите сумму больше нуля');
      return;
    }

    Navigator.of(context).pop(_PaymentInput(value, _method));
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(
        20,
        0,
        20,
        20 + MediaQuery.of(context).viewInsets.bottom,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Text(
            'Получена оплата',
            style: TextStyle(fontSize: 20, fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 4),
          Text(
            '${widget.order.shopName} · к оплате ${Formatters.money(widget.order.totalAmount)}',
            style: const TextStyle(color: AppColors.muted, fontWeight: FontWeight.w500),
          ),
          const SizedBox(height: 20),
          TextField(
            controller: _amount,
            autofocus: true,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]'))],
            style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 20),
            decoration: InputDecoration(
              hintText: 'Сумма',
              suffixText: 'сом.',
              errorText: _error,
            ),
          ),
          const SizedBox(height: 16),
          SegmentedButton<PaymentMethod>(
            showSelectedIcon: false,
            style: SegmentedButton.styleFrom(
              selectedBackgroundColor: AppDecor.heroMid,
              selectedForegroundColor: Colors.white,
              foregroundColor: AppColors.brown,
              minimumSize: const Size.fromHeight(48),
              textStyle: const TextStyle(fontWeight: FontWeight.w800),
            ),
            segments: [
              for (final method in PaymentMethod.values)
                ButtonSegment(value: method, label: Text(method.label)),
            ],
            selected: {_method},
            onSelectionChanged: (value) => setState(() => _method = value.first),
          ),
          const SizedBox(height: 20),
          PrimaryButton(
            label: 'Подтвердить доставку',
            icon: AppIcons.success,
            onPressed: _submit,
          ),
        ],
      ),
    );
  }
}