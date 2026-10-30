import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:non_app/core/models/paged_result.dart';
import 'package:non_app/features/shops/domain/usecases/create_shop_usecase.dart';
import 'package:non_app/features/shops/domain/usecases/get_shop_balance_usecase.dart';
import 'package:non_app/features/shops/domain/usecases/get_shops_usecase.dart';

import '../../../../core/providers/core_providers.dart';
import '../../../auth/presentation/providers/auth_providers.dart';
import '../../data/datasources/shops_remote_data_source.dart';
import '../../data/repository/shops_repository_impl.dart';
import '../../domain/entities/shop.dart';
import '../../domain/entities/shop_balance.dart';
import '../../domain/repository/shops_repository.dart';
import '../../domain/usecases/create_my_shop_usecase.dart';
import '../../domain/usecases/get_my_balance_usecase.dart';
import '../../domain/usecases/get_my_shop_usecase.dart';

final shopsRepositoryProvider = Provider<ShopsRepository>((ref) {
  return ShopsRepositoryImpl(ShopsRemoteDataSource(ref.watch(apiClientProvider)));
});

final getMyShopUseCaseProvider =
    Provider((ref) => GetMyShopUseCase(ref.watch(shopsRepositoryProvider)));
final createMyShopUseCaseProvider =
    Provider((ref) => CreateMyShopUseCase(ref.watch(shopsRepositoryProvider)));
final getMyBalanceUseCaseProvider =
    Provider((ref) => GetMyBalanceUseCase(ref.watch(shopsRepositoryProvider)));

final myShopProvider = AsyncNotifierProvider<MyShopNotifier, Shop?>(MyShopNotifier.new);

class MyShopNotifier extends AsyncNotifier<Shop?> {
  @override
  Future<Shop?> build() {
    ref.watch(currentUserIdProvider);

    return ref.read(getMyShopUseCaseProvider)();
  }

  Future<void> create({
    required String name,
    required String address,
    required String phone,
    String? contactName,
  }) async {
    final shop = await ref.read(createMyShopUseCaseProvider)(
      name: name,
      address: address,
      phone: phone,
      contactName: contactName,
    );

    state = AsyncData<Shop?>(shop);
  }
}

final myBalanceProvider = FutureProvider.autoDispose<ShopBalance>((ref) {
  ref.watch(currentUserIdProvider);

  return ref.read(getMyBalanceUseCaseProvider)();
});

final getShopsUseCaseProvider =
    Provider((ref) => GetShopsUseCase(ref.watch(shopsRepositoryProvider)));
final createShopUseCaseProvider =
    Provider((ref) => CreateShopUseCase(ref.watch(shopsRepositoryProvider)));
final getShopBalanceUseCaseProvider =
    Provider((ref) => GetShopBalanceUseCase(ref.watch(shopsRepositoryProvider)));

final shopSearchProvider =
    NotifierProvider<ShopSearchNotifier, String>(ShopSearchNotifier.new);

class ShopSearchNotifier extends Notifier<String> {
  @override
  String build() => '';

  void set(String value) => state = value;
}

final shopsProvider = FutureProvider.autoDispose<PagedResult<Shop>>((ref) {
  ref.watch(currentUserIdProvider);
  final search = ref.watch(shopSearchProvider).trim();

  return ref.read(getShopsUseCaseProvider)(
    search: search.isEmpty ? null : search,
    page: 1,
    pageSize: 100,
  );
});

final shopBalanceProvider =
    FutureProvider.autoDispose.family<ShopBalance, String>((ref, id) {
  return ref.read(getShopBalanceUseCaseProvider)(id);
});