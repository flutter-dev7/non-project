import '../repository/auth_repository.dart';

class ResetPasswordUseCase {
  const ResetPasswordUseCase(this._repository);

  final AuthRepository _repository;

  Future<void> call({required String resetToken, required String newPassword}) {
    return _repository.resetPassword(resetToken: resetToken, newPassword: newPassword);
  }
}