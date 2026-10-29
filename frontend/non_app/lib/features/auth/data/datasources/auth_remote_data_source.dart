import '../../../../core/network/api_client.dart';
import '../models/auth_response_model.dart';
import '../models/auth_user_model.dart';

class AuthRemoteDataSource {
  AuthRemoteDataSource(this._client);

  final ApiClient _client;

  Future<void> register({
    required String firstName,
    required String lastName,
    required String phone,
    required String email,
    required String password,
  }) async {
    await _client.post(
      '/api/auth/register',
      auth: false,
      data: {
        'firstName': firstName,
        'lastName': lastName,
        'phone': phone,
        'email': email,
        'password': password,
      },
    );
  }

  Future<AuthResponseModel> verifyEmail({required String email, required String code}) async {
    final data = await _client.post(
      '/api/auth/verify-email',
      auth: false,
      data: {'email': email, 'code': code},
    );

    return AuthResponseModel.fromJson(data as Map<String, dynamic>);
  }

  Future<void> resendVerification(String email) async {
    await _client.post('/api/auth/resend-verification', auth: false, data: {'email': email});
  }

  Future<AuthResponseModel> login({required String email, required String password}) async {
    final data = await _client.post(
      '/api/auth/login',
      auth: false,
      data: {'email': email, 'password': password},
    );

    return AuthResponseModel.fromJson(data as Map<String, dynamic>);
  }

  Future<void> forgotPassword(String email) async {
    await _client.post('/api/auth/forgot-password', auth: false, data: {'email': email});
  }

  Future<String> verifyResetCode({required String email, required String code}) async {
    final data = await _client.post(
      '/api/auth/verify-reset-code',
      auth: false,
      data: {'email': email, 'code': code},
    );

    return (data as Map<String, dynamic>)['resetToken'] as String;
  }

  Future<void> resetPassword({required String resetToken, required String newPassword}) async {
    await _client.post(
      '/api/auth/reset-password',
      auth: false,
      data: {'resetToken': resetToken, 'newPassword': newPassword},
    );
  }

  Future<AuthResponseModel> changePassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    final data = await _client.post(
      '/api/auth/change-password',
      data: {'currentPassword': currentPassword, 'newPassword': newPassword},
    );

    return AuthResponseModel.fromJson(data as Map<String, dynamic>);
  }

  Future<AuthUserModel> me() async {
    final data = await _client.get('/api/auth/me');

    return AuthUserModel.fromJson(data as Map<String, dynamic>);
  }

  Future<void> logout(String refreshToken) async {
    await _client.post('/api/auth/logout', auth: false, data: {'refreshToken': refreshToken});
  }

  Future<void> logoutAll() async {
    await _client.post('/api/auth/logout-all');
  }
}