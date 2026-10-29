import '../repository/auth_repository.dart';

class LogoutAllUseCase {
  const LogoutAllUseCase(this._repository);

  final AuthRepository _repository;

  Future<void> call() => _repository.logoutAll();
}