import '../entities/auth_user.dart';
import '../repository/auth_repository.dart';

class VerifyEmailUseCase {
  const VerifyEmailUseCase(this._repository);

  final AuthRepository _repository;

  Future<AuthUser> call({required String email, required String code}) {
    return _repository.verifyEmail(email: email, code: code);
  }
}