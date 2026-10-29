import '../entities/auth_user.dart';

abstract class AuthRepository {
  Future<AuthUser?> restoreSession();

  Future<void> register({
    required String firstName,
    required String lastName,
    required String phone,
    required String email,
    required String password,
  });

  Future<AuthUser> verifyEmail({required String email, required String code});

  Future<void> resendVerification(String email);

  Future<AuthUser> login({required String email, required String password});

  Future<void> forgotPassword(String email);

  Future<String> verifyResetCode({required String email, required String code});

  Future<void> resetPassword({required String resetToken, required String newPassword});

  Future<AuthUser> changePassword({
    required String currentPassword,
    required String newPassword,
  });

  Future<void> logout();

  Future<void> logoutAll();
}