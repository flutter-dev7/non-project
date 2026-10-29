import '../repository/auth_repository.dart';

class RegisterUseCase {
  const RegisterUseCase(this._repository);

  final AuthRepository _repository;

  Future<void> call({
    required String firstName,
    required String lastName,
    required String phone,
    required String email,
    required String password,
  }) {
    return _repository.register(
      firstName: firstName,
      lastName: lastName,
      phone: phone,
      email: email,
      password: password,
    );
  }
}