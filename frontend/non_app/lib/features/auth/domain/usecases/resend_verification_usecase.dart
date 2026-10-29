import '../repository/auth_repository.dart';

class ResendVerificationUseCase {
  const ResendVerificationUseCase(this._repository);

  final AuthRepository _repository;

  Future<void> call(String email) => _repository.resendVerification(email);
}