import '../repository/auth_repository.dart';

class VerifyResetCodeUseCase {
  const VerifyResetCodeUseCase(this._repository);

  final AuthRepository _repository;

  /// Возвращает resetToken.
  Future<String> call({required String email, required String code}) {
    return _repository.verifyResetCode(email: email, code: code);
  }
}