import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:uuid/uuid.dart';

import '../../../../core/constants/app_icons.dart';
import '../../../../core/router/app_routes.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_decor.dart';
import '../../../../core/utils/app_snackbar.dart';
import '../../../../core/utils/formatters.dart';
import '../../../../core/utils/submit_state_mixin.dart';
import '../../../../core/widgets/app_text_field.dart';
import '../../../../core/widgets/app_top_bar.dart';
import '../../../../core/widgets/inline_message.dart';
import '../../../../core/widgets/primary_button.dart';
import '../../../../core/widgets/quantity_stepper.dart';
import '../../../products/domain/entities/product.dart';
import '../../../products/presentation/providers/products_providers.dart';
import '../../../settings/presentation/providers/settings_providers.dart';
import '../../../shops/presentation/providers/shops_providers.dart';
import '../../domain/entities/order.dart';
import '../../domain/entities/order_draft.dart';
import '../../domain/entities/payment_type.dart';
import '../providers/orders_providers.dart';
import '../utils/delivery_rules.dart';

class OrderFormPage extends ConsumerStatefulWidget {
  const OrderFormPage({super.key, this.order});

  /// null — новый заказ, иначе изменение существующего.
  final Order? order;

  @override
  ConsumerState<OrderFormPage> createState() => _OrderFormPageState();
}

class _OrderFormPageState extends ConsumerState<OrderFormPage>
    with SubmitStateMixin<OrderFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _note = TextEditingController();
  final String _orderId = const Uuid().v4();

  late Map<String, OrderLine> _lines;
  DateTime? _picked;
  PaymentType _payment = PaymentType.now;

  bool get _isEdit => widget.order != null;

  @override
  void initState() {
    super.initState();

    final order = widget.order;

    if (order != null) {
      _lines = {
        for (final item in order.items)
          item.productId: OrderLine(
            productId: item.productId,
            name: item.productName,
            unitPrice: item.unitPrice,
            quantity: item.quantity,
          ),
      };
      _picked = order.deliveryDate;
      _payment = order.paymentType;
      _note.text = order.note ?? '';
    } else {
      _lines = Map.of(ref.read(cartProvider));
    }
  }

  @override
  void dispose() {
    _note.dispose();
    super.dispose();
  }

  void _setLine(OrderLine line) {
    setState(() {
      if (line.quantity <= 0) {
        _lines.remove(line.productId);
      } else {
        _lines[line.productId] = line;
      }
    });

    if (!_isEdit) {
      ref.read(cartProvider.notifier).replaceAll(_lines);
    }
  }

  double get _total => _lines.values.fold(0.0, (sum, line) => sum + line.total);

  Future<void> _addProduct() async {
    final product = await showModalBottomSheet<Product>(
      context: context,
      showDragHandle: true,
      backgroundColor: Colors.white,
      isScrollControlled: true,
      builder: (_) => const _ProductPicker(),
    );

    if (product == null) return;

    final current = _lines[product.id];

    _setLine(
      OrderLine(
        productId: product.id,
        name: product.name,
        unitPrice: product.price,
        quantity: (current?.quantity ?? 0) + 1,
      ),
    );
  }

  Future<void> _pickDate(DateTime min) async {
    final max = DeliveryRules.maxDate();
    final current = _picked ?? min;
    final first = current.isBefore(min) ? current : min;
    final initial = current.isAfter(max) ? max : current;

    final result = await showDatePicker(
      context: context,
      initialDate: initial,
      firstDate: first,
      lastDate: max,
    );

    if (result != null) {
      setState(() => _picked = result);
    }
  }

  Future<void> _submit(DateTime date) {
    if (_lines.isEmpty) {
      setState(() => error = 'Добавьте хотя бы один товар');
      return Future.value();
    }

    return submit(_formKey, () async {
      final note = _note.text.trim();

      final draft = OrderDraft(
        deliveryDate: date,
        paymentType: _payment,
        note: note.isEmpty ? null : note,
        lines: _lines.values.toList(),
      );

      if (_isEdit) {
        await ref.read(updateOrderUseCaseProvider)(widget.order!.id, draft);
        ref.invalidate(orderDetailsProvider(widget.order!.id));
      } else {
        await ref.read(createOrderUseCaseProvider)(_orderId, draft);
        ref.read(cartProvider.notifier).clear();
      }

      ref.invalidate(ordersProvider);
      ref.invalidate(myBalanceProvider);

      if (!mounted) return;

      if (_isEdit) {
        AppSnackBar.show(context, 'Заказ обновлён');
        context.pop();
      } else {
        AppSnackBar.show(context, 'Заказ оформлен');
        context.pushReplacement(AppRoutes.orderDetailsFor(_orderId));
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final cutoff = ref.watch(orderCutoffProvider).valueOrNull;
    final minDate = DeliveryRules.minDate(cutoff);
    final date = _picked ?? minDate;

    return Scaffold(
      appBar: AppTopBar(title: _isEdit ? 'Изменить заказ' : 'Оформление заказа'),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
        children: [
          _sectionTitle('Товары'),
          if (_lines.isEmpty)
            const Card(
              child: Padding(
                padding: EdgeInsets.all(20),
                child: Text(
                  'Корзина пуста',
                  style: TextStyle(color: AppColors.muted, fontWeight: FontWeight.w600),
                ),
              ),
            )
          else
            for (final line in _lines.values) ...[
              _LineTile(line: line, onChanged: _setLine),
              const SizedBox(height: 10),
            ],
          const SizedBox(height: 4),
          OutlinedButton.icon(
            onPressed: loading ? null : _addProduct,
            icon: const Icon(AppIcons.plus, size: 18),
            label: const Text('Добавить товар'),
          ),
          const SizedBox(height: 24),
          _sectionTitle('Дата доставки'),
          Material(
            color: AppDecor.fieldFill,
            borderRadius: BorderRadius.circular(16),
            child: InkWell(
              borderRadius: BorderRadius.circular(16),
              onTap: loading ? null : () => _pickDate(minDate),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Row(
                  children: [
                    const Icon(AppIcons.calendar, color: AppDecor.heroMid),
                    const SizedBox(width: 12),
                    Text(
                      Formatters.date(date),
                      style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
                    ),
                  ],
                ),
              ),
            ),
          ),
          if (cutoff != null) ...[
            const SizedBox(height: 8),
            Text(
              'Заказы на завтра принимаются до $cutoff.',
              style: const TextStyle(color: AppColors.muted, fontSize: 13),
            ),
          ],
          const SizedBox(height: 24),
          _sectionTitle('Оплата'),
          SizedBox(
            width: double.infinity,
            child: SegmentedButton<PaymentType>(
              showSelectedIcon: false,
              style: SegmentedButton.styleFrom(
                selectedBackgroundColor: AppDecor.heroMid,
                selectedForegroundColor: Colors.white,
                foregroundColor: AppColors.brown,
                minimumSize: const Size.fromHeight(48),
                textStyle: const TextStyle(fontWeight: FontWeight.w800),
              ),
              segments: [
                for (final type in PaymentType.values)
                  ButtonSegment(value: type, label: Text(type.label)),
              ],
              selected: {_payment},
              onSelectionChanged:
                  loading ? null : (value) => setState(() => _payment = value.first),
            ),
          ),
          const SizedBox(height: 24),
          Form(
            key: _formKey,
            child: AppTextField(
              controller: _note,
              label: 'Комментарий',
              hint: 'Необязательно',
              icon: AppIcons.note,
              textInputAction: TextInputAction.done,
              validator: (value) =>
                  (value ?? '').length > 1000 ? 'Не более 1000 символов' : null,
              enabled: !loading,
            ),
          ),
          if (error != null) ...[
            const SizedBox(height: 16),
            InlineMessage(message: error!),
          ],
          const SizedBox(height: 24),
          Row(
            children: [
              const Text('Итого', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 16)),
              const Spacer(),
              Text(
                Formatters.money(_total),
                style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 22),
              ),
            ],
          ),
          const SizedBox(height: 16),
          PrimaryButton(
            label: _isEdit ? 'Сохранить изменения' : 'Оформить заказ',
            onPressed: () => _submit(date),
            isLoading: loading,
          ),
        ],
      ),
    );
  }

  Widget _sectionTitle(String text) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Text(text, style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 17)),
    );
  }
}

class _LineTile extends StatelessWidget {
  const _LineTile({required this.line, required this.onChanged});

  final OrderLine line;
  final ValueChanged<OrderLine> onChanged;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    line.name,
                    style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 16),
                  ),
                ),
                InkWell(
                  onTap: () => onChanged(line.withQuantity(0)),
                  child: const Padding(
                    padding: EdgeInsets.all(4),
                    child: Icon(AppIcons.trash, size: 20, color: AppColors.muted),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
            Row(
              children: [
                Text(
                  Formatters.money(line.unitPrice),
                  style: const TextStyle(color: AppColors.muted, fontWeight: FontWeight.w600),
                ),
                const Spacer(),
                QuantityStepper(
                  value: line.quantity,
                  min: 1,
                  onChanged: (value) => onChanged(line.withQuantity(value)),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Align(
              alignment: Alignment.centerRight,
              child: Text(
                Formatters.money(line.total),
                style: const TextStyle(fontWeight: FontWeight.w800),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ProductPicker extends ConsumerWidget {
  const _ProductPicker();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final products = ref.watch(productsProvider);

    return SafeArea(
      child: ConstrainedBox(
        constraints: BoxConstraints(maxHeight: MediaQuery.of(context).size.height * 0.7),
        child: products.when(
          loading: () => const Padding(
            padding: EdgeInsets.all(32),
            child: Center(child: CircularProgressIndicator()),
          ),
          error: (_, __) => const Padding(
            padding: EdgeInsets.all(32),
            child: Center(child: Text('Не удалось загрузить товары')),
          ),
          data: (list) => ListView.separated(
            shrinkWrap: true,
            padding: const EdgeInsets.fromLTRB(20, 0, 20, 20),
            itemCount: list.length,
            separatorBuilder: (_, __) => const Divider(),
            itemBuilder: (context, index) {
              final product = list[index];

              return ListTile(
                contentPadding: EdgeInsets.zero,
                title: Text(product.name, style: const TextStyle(fontWeight: FontWeight.w700)),
                subtitle: Text(Formatters.money(product.price)),
                trailing: const Icon(AppIcons.plus, color: AppDecor.heroMid),
                onTap: () => Navigator.of(context).pop(product),
              );
            },
          ),
        ),
      ),
    );
  }
}