import '../repository/auth_repository.dart';

class ForgotPasswordUseCase {
  const ForgotPasswordUseCase(this._repository);

  final AuthRepository _repository;

  Future<void> call(String email) => _repository.forgotPassword(email);
}