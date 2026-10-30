import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:non_app/features/shops/domain/entities/shop.dart';
import 'package:non_app/features/visits/presentation/providers/visits_providers.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/utils/app_snackbar.dart';


Future<void> recordVisit(BuildContext context, WidgetRef ref, Shop shop) async {
  final note = await showDialog<String>(
    context: context,
    builder: (_) => _VisitNoteDialog(shopName: shop.name),
  );

  if (note == null) return;

  try {
    await ref.read(createVisitUseCaseProvider)(
      shopId: shop.id,
      note: note.isEmpty ? null : note,
    );

    ref.invalidate(visitsProvider);

    if (context.mounted) {
      AppSnackBar.show(context, 'Визит отмечен: ${shop.name}');
    }
  } on ApiException catch (e) {
    if (context.mounted) {
      AppSnackBar.show(context, e.message, isError: true);
    }
  }
}

class _VisitNoteDialog extends StatefulWidget {
  const _VisitNoteDialog({required this.shopName});

  final String shopName;

  @override
  State<_VisitNoteDialog> createState() => _VisitNoteDialogState();
}

class _VisitNoteDialogState extends State<_VisitNoteDialog> {
  final _controller = TextEditingController();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      backgroundColor: Colors.white,
      surfaceTintColor: Colors.transparent,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(24)),
      title: Text(
        'Визит: ${widget.shopName}',
        style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
      ),
      content: TextField(
        controller: _controller,
        autofocus: true,
        maxLines: 3,
        maxLength: 1000,
        decoration: const InputDecoration(hintText: 'Заметка (необязательно)'),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Отмена'),
        ),
        FilledButton(
          style: FilledButton.styleFrom(minimumSize: const Size(0, 46)),
          onPressed: () => Navigator.of(context).pop(_controller.text.trim()),
          child: const Text('Сохранить'),
        ),
      ],
    );
  }
}