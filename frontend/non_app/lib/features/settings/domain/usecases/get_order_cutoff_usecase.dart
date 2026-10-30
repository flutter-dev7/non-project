import '../repository/settings_repository.dart';

class GetOrderCutoffUseCase {
  const GetOrderCutoffUseCase(this._repository);

  final SettingsRepository _repository;

  Future<String> call() => _repository.getOrderCutoffTime();
}