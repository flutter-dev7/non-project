import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class TokenStorage {
  TokenStorage({FlutterSecureStorage? storage})
      : _storage = storage ?? const FlutterSecureStorage();

  static const String _accessKey = 'access_token';
  static const String _refreshKey = 'refresh_token';
  static const String _userKey = 'cached_user';

  final FlutterSecureStorage _storage;

  String? _access;
  String? _refresh;
  String? _user;
  bool _loaded = false;

  Future<void> _ensureLoaded() async {
    if (_loaded) return;

    _access = await _storage.read(key: _accessKey);
    _refresh = await _storage.read(key: _refreshKey);
    _user = await _storage.read(key: _userKey);
    _loaded = true;
  }

  Future<String?> readAccessToken() async {
    await _ensureLoaded();
    return _access;
  }

  Future<String?> readRefreshToken() async {
    await _ensureLoaded();
    return _refresh;
  }

  Future<String?> readUser() async {
    await _ensureLoaded();
    return _user;
  }

  Future<void> saveTokens({required String access, required String refresh}) async {
    await _ensureLoaded();

    _access = access;
    _refresh = refresh;

    await _storage.write(key: _accessKey, value: access);
    await _storage.write(key: _refreshKey, value: refresh);
  }

  Future<void> saveUser(String json) async {
    await _ensureLoaded();

    _user = json;
    await _storage.write(key: _userKey, value: json);
  }

  Future<void> clear() async {
    _access = null;
    _refresh = null;
    _user = null;
    _loaded = true;

    await _storage.delete(key: _accessKey);
    await _storage.delete(key: _refreshKey);
    await _storage.delete(key: _userKey);
  }
}