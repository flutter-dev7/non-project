import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/providers/core_providers.dart';
import '../../data/datasources/auth_remote_data_source.dart';
import '../../data/repository/auth_repository_impl.dart';
import '../../domain/entities/auth_user.dart';
import '../../domain/repository/auth_repository.dart';
import '../../domain/usecases/change_password_usecase.dart';
import '../../domain/usecases/forgot_password_usecase.dart';
import '../../domain/usecases/login_usecase.dart';
import '../../domain/usecases/logout_all_usecase.dart';
import '../../domain/usecases/logout_usecase.dart';
import '../../domain/usecases/register_usecase.dart';
import '../../domain/usecases/resend_verification_usecase.dart';
import '../../domain/usecases/reset_password_usecase.dart';
import '../../domain/usecases/restore_session_usecase.dart';
import '../../domain/usecases/verify_email_usecase.dart';
import '../../domain/usecases/verify_reset_code_usecase.dart';

final authRemoteDataSourceProvider = Provider<AuthRemoteDataSource>((ref) {
  return AuthRemoteDataSource(ref.watch(apiClientProvider));
});

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepositoryImpl(
    remote: ref.watch(authRemoteDataSourceProvider),
    tokens: ref.watch(tokenStorageProvider),
  );
});

final loginUseCaseProvider =
    Provider((ref) => LoginUseCase(ref.watch(authRepositoryProvider)));
final registerUseCaseProvider =
    Provider((ref) => RegisterUseCase(ref.watch(authRepositoryProvider)));
final verifyEmailUseCaseProvider =
    Provider((ref) => VerifyEmailUseCase(ref.watch(authRepositoryProvider)));
final resendVerificationUseCaseProvider =
    Provider((ref) => ResendVerificationUseCase(ref.watch(authRepositoryProvider)));
final forgotPasswordUseCaseProvider =
    Provider((ref) => ForgotPasswordUseCase(ref.watch(authRepositoryProvider)));
final verifyResetCodeUseCaseProvider =
    Provider((ref) => VerifyResetCodeUseCase(ref.watch(authRepositoryProvider)));
final resetPasswordUseCaseProvider =
    Provider((ref) => ResetPasswordUseCase(ref.watch(authRepositoryProvider)));
final changePasswordUseCaseProvider =
    Provider((ref) => ChangePasswordUseCase(ref.watch(authRepositoryProvider)));
final restoreSessionUseCaseProvider =
    Provider((ref) => RestoreSessionUseCase(ref.watch(authRepositoryProvider)));
final logoutUseCaseProvider =
    Provider((ref) => LogoutUseCase(ref.watch(authRepositoryProvider)));
final logoutAllUseCaseProvider =
    Provider((ref) => LogoutAllUseCase(ref.watch(authRepositoryProvider)));

final authProvider = AsyncNotifierProvider<AuthNotifier, AuthUser?>(AuthNotifier.new);

class AuthNotifier extends AsyncNotifier<AuthUser?> {
  @override
  Future<AuthUser?> build() async {
    final user = await ref.read(restoreSessionUseCaseProvider)();

    final subscription = ref.read(sessionEventsProvider).onExpired.listen((_) {
      state = const AsyncData<AuthUser?>(null);
    });
    ref.onDispose(subscription.cancel);

    return user;
  }

  Future<void> login(String email, String password) async {
    final user = await ref.read(loginUseCaseProvider)(email: email, password: password);
    state = AsyncData<AuthUser?>(user);
  }

  Future<void> verifyEmail(String email, String code) async {
    final user = await ref.read(verifyEmailUseCaseProvider)(email: email, code: code);
    state = AsyncData<AuthUser?>(user);
  }

  Future<void> changePassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    final user = await ref.read(changePasswordUseCaseProvider)(
      currentPassword: currentPassword,
      newPassword: newPassword,
    );
    state = AsyncData<AuthUser?>(user);
  }

  Future<void> logout() async {
    await ref.read(logoutUseCaseProvider)();
    state = const AsyncData<AuthUser?>(null);
  }

  Future<void> logoutAll() async {
    await ref.read(logoutAllUseCaseProvider)();
    state = const AsyncData<AuthUser?>(null);
  }
}