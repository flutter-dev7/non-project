import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers/core_providers.dart';
import '../../../../core/utils/formatters.dart';
import '../../../auth/presentation/providers/auth_providers.dart';
import '../../data/datasources/visits_remote_data_source.dart';
import '../../data/repository/visits_repository_impl.dart';
import '../../domain/entities/visit.dart';
import '../../domain/repository/visits_repository.dart';
import '../../domain/usecases/create_visit_usecase.dart';
import '../../domain/usecases/get_visits_usecase.dart';

final visitsRepositoryProvider = Provider<VisitsRepository>((ref) {
  return VisitsRepositoryImpl(VisitsRemoteDataSource(ref.watch(apiClientProvider)));
});

final getVisitsUseCaseProvider =
    Provider((ref) => GetVisitsUseCase(ref.watch(visitsRepositoryProvider)));
final createVisitUseCaseProvider =
    Provider((ref) => CreateVisitUseCase(ref.watch(visitsRepositoryProvider)));

/// Визиты за последние 30 дней, новые сверху.
final visitsProvider = FutureProvider.autoDispose<List<Visit>>((ref) async {
  ref.watch(currentUserIdProvider);

  final today = Formatters.today();

  final page = await ref.read(getVisitsUseCaseProvider)(
    dateFrom: DateTime(today.year, today.month, today.day - 30),
    dateTo: today,
    page: 1,
    pageSize: 100,
  );

  final list = [...page.items];
  list.sort((a, b) => b.visitedAt.compareTo(a.visitedAt));

  return list;
});