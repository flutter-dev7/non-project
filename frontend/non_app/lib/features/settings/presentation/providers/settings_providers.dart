import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers/core_providers.dart';
import '../../data/datasources/settings_remote_data_source.dart';
import '../../data/repository/settings_repository_impl.dart';
import '../../domain/repository/settings_repository.dart';
import '../../domain/usecases/get_order_cutoff_usecase.dart';

final settingsRepositoryProvider = Provider<SettingsRepository>((ref) {
  return SettingsRepositoryImpl(SettingsRemoteDataSource(ref.watch(apiClientProvider)));
});

final getOrderCutoffUseCaseProvider =
    Provider((ref) => GetOrderCutoffUseCase(ref.watch(settingsRepositoryProvider)));

final orderCutoffProvider = FutureProvider.autoDispose<String>((ref) {
  return ref.watch(getOrderCutoffUseCaseProvider)();
});