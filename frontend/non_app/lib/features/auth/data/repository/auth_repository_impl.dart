import 'dart:convert';

import '../../../../core/network/api_exception.dart';
import '../../../../core/storage/token_storage.dart';
import '../../domain/entities/auth_user.dart';
import '../../domain/repository/auth_repository.dart';
import '../datasources/auth_remote_data_source.dart';
import '../models/auth_response_model.dart';
import '../models/auth_user_model.dart';

class AuthRepositoryImpl implements AuthRepository {
  AuthRepositoryImpl({
    required AuthRemoteDataSource remote,
    required TokenStorage tokens,
  })  : _remote = remote,
        _tokens = tokens;

  final AuthRemoteDataSource _remote;
  final TokenStorage _tokens;

  @override
  Future<AuthUser?> restoreSession() async {
    final refreshToken = await _tokens.readRefreshToken();
    if (refreshToken == null) return null;

    try {
      final user = await _remote.me();
      await _tokens.saveUser(jsonEncode(user.toJson()));

      return user;
    } on ApiException catch (e) {
      final sessionIsDead = e.type == ApiErrorType.unauthorized ||
          e.type == ApiErrorType.forbidden ||
          e.type == ApiErrorType.notFound;

      if (sessionIsDead) {
        await _tokens.clear();
        return null;
      }

      return _readCachedUser();
    }
  }

  Future<AuthUser?> _readCachedUser() async {
    final cached = await _tokens.readUser();
    if (cached == null) return null;

    try {
      return AuthUserModel.fromJson(jsonDecode(cached) as Map<String, dynamic>);
    } on FormatException {
      return null;
    }
  }

  Future<AuthUser> _saveSession(AuthResponseModel response) async {
    await _tokens.saveTokens(
      access: response.accessToken,
      refresh: response.refreshToken,
    );
    await _tokens.saveUser(jsonEncode(response.user.toJson()));

    return response.user;
  }

  @override
  Future<void> register({
    required String firstName,
    required String lastName,
    required String phone,
    required String email,
    required String password,
  }) {
    return _remote.register(
      firstName: firstName,
      lastName: lastName,
      phone: phone,
      email: email,
      password: password,
    );
  }

  @override
  Future<AuthUser> verifyEmail({required String email, required String code}) async {
    return _saveSession(await _remote.verifyEmail(email: email, code: code));
  }

  @override
  Future<void> resendVerification(String email) => _remote.resendVerification(email);

  @override
  Future<AuthUser> login({required String email, required String password}) async {
    return _saveSession(await _remote.login(email: email, password: password));
  }

  @override
  Future<void> forgotPassword(String email) => _remote.forgotPassword(email);

  @override
  Future<String> verifyResetCode({required String email, required String code}) {
    return _remote.verifyResetCode(email: email, code: code);
  }

  @override
  Future<void> resetPassword({required String resetToken, required String newPassword}) {
    return _remote.resetPassword(resetToken: resetToken, newPassword: newPassword);
  }

  @override
  Future<AuthUser> changePassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    final response = await _remote.changePassword(
      currentPassword: currentPassword,
      newPassword: newPassword,
    );

    return _saveSession(response);
  }

  @override
  Future<void> logout() async {
    final refreshToken = await _tokens.readRefreshToken();

    try {
      if (refreshToken != null) {
        await _remote.logout(refreshToken);
      }
    } on ApiException {
      // Сессию всё равно закрываем локально.
    } finally {
      await _tokens.clear();
    }
  }

  @override
  Future<void> logoutAll() async {
    await _remote.logoutAll();
    await _tokens.clear();
  }
}