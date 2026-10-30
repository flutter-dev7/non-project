import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/constants/app_icons.dart';
import '../../../../core/models/paged_result.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_decor.dart';
import '../../../../core/utils/error_text.dart';
import '../../domain/entities/shop.dart';
import '../providers/shops_providers.dart';

Future<Shop?> pickShop(BuildContext context) {
  return showModalBottomSheet<Shop>(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    backgroundColor: Colors.white,
    builder: (_) => const _ShopPicker(),
  );
}

class _ShopPicker extends ConsumerStatefulWidget {
  const _ShopPicker();

  @override
  ConsumerState<_ShopPicker> createState() => _ShopPickerState();
}

class _ShopPickerState extends ConsumerState<_ShopPicker> {
  final _search = TextEditingController();
  Timer? _debounce;
  late Future<PagedResult<Shop>> _future;

  @override
  void initState() {
    super.initState();
    _future = _load('');
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _search.dispose();
    super.dispose();
  }

  Future<PagedResult<Shop>> _load(String query) {
    final text = query.trim();

    return ref.read(getShopsUseCaseProvider)(
      search: text.isEmpty ? null : text,
      page: 1,
      pageSize: 50,
    );
  }

  void _onChanged(String value) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 400), () {
      if (!mounted) return;

      final future = _load(value);

      setState(() {
        _future = future;
      });
    });
  }

  @override
  Widget build(BuildContext context) {
    final height = MediaQuery.of(context).size.height * 0.75;

    return Padding(
      padding: EdgeInsets.only(
        bottom: MediaQuery.of(context).viewInsets.bottom,
      ),
      child: SizedBox(
        height: height,
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 0, 20, 12),
              child: TextField(
                controller: _search,
                onChanged: _onChanged,
                decoration: const InputDecoration(
                  hintText: 'Поиск магазина',
                  prefixIcon: Icon(AppIcons.search, size: 22),
                ),
              ),
            ),
            Expanded(
              child: FutureBuilder<PagedResult<Shop>>(
                future: _future,
                builder: (context, snapshot) {
                  if (snapshot.connectionState != ConnectionState.done) {
                    return const Center(child: CircularProgressIndicator());
                  }

                  if (snapshot.hasError) {
                    return Center(child: Text(errorText(snapshot.error!)));
                  }

                  final shops = snapshot.data!.items;

                  if (shops.isEmpty) {
                    return const Center(
                      child: Text(
                        'Магазины не найдены',
                        style: TextStyle(
                          color: AppColors.muted,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    );
                  }

                  return ListView.separated(
                    padding: const EdgeInsets.fromLTRB(20, 0, 20, 20),
                    itemCount: shops.length,
                    separatorBuilder: (_, __) => const Divider(),
                    itemBuilder: (context, index) {
                      final shop = shops[index];

                      return ListTile(
                        contentPadding: EdgeInsets.zero,
                        leading: const Icon(
                          AppIcons.store,
                          color: AppDecor.heroMid,
                        ),
                        title: Text(
                          shop.name,
                          style: const TextStyle(fontWeight: FontWeight.w700),
                        ),
                        subtitle: Text(shop.address),
                        onTap: () => Navigator.of(context).pop(shop),
                      );
                    },
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}
