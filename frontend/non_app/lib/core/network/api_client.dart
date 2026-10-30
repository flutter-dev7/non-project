import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';

import '../constants/api_constants.dart';
import '../storage/token_storage.dart';
import 'api_exception.dart';
import 'session_events.dart';

enum _RefreshOutcome { success, rejected, failed }

class ApiClient {
  ApiClient({
    required TokenStorage tokenStorage,
    required SessionEvents sessionEvents,
    String? baseUrl,
  }) : _tokenStorage = tokenStorage,
       _sessionEvents = sessionEvents,
       dio = Dio(_options(baseUrl)),
       _refreshDio = Dio(_options(baseUrl)) {
    dio.interceptors.add(
      InterceptorsWrapper(onRequest: _onRequest, onError: _onError),
    );
  }

  final Dio dio;
  final Dio _refreshDio;
  final TokenStorage _tokenStorage;
  final SessionEvents _sessionEvents;

  Future<_RefreshOutcome>? _refreshing;

  static BaseOptions _options(String? baseUrl) => BaseOptions(
    baseUrl: baseUrl ?? ApiConstants.baseUrl,
    connectTimeout: ApiConstants.connectTimeout,
    receiveTimeout: ApiConstants.receiveTimeout,
    headers: const {'Accept': 'application/json'},
  );

  Future<dynamic> get(
    String path, {
    Map<String, dynamic>? query,
    bool auth = true,
  }) => _request('GET', path, query: query, auth: auth);

  Future<dynamic> post(String path, {Object? data, bool auth = true}) =>
      _request('POST', path, data: data, auth: auth);

  Future<dynamic> put(String path, {Object? data, bool auth = true}) =>
      _request('PUT', path, data: data, auth: auth);

  Future<dynamic> delete(String path, {Object? data, bool auth = true}) =>
      _request('DELETE', path, data: data, auth: auth);

  Future<dynamic> _request(
    String method,
    String path, {
    Object? data,
    Map<String, dynamic>? query,
    bool auth = true,
  }) async {
    Future<Response<dynamic>> send() {
      return dio.request<dynamic>(
        path,
        data: data,
        queryParameters: query,
        options: Options(method: method, extra: {'skipAuth': !auth}),
      );
    }

    try {
      Response<dynamic> response;

      try {
        response = await send();
      } on DioException catch (e) {
        final isConnectionProblem =
            e.type == DioExceptionType.connectionTimeout ||
            e.type == DioExceptionType.connectionError;

        if (method != 'GET' || !isConnectionProblem) rethrow;

        response = await send();
      }

      return _unwrap(response);
    } on DioException catch (e) {
      debugPrint('API ERROR ${e.type} ${e.message} $path');
      throw ApiException.fromDio(e);
    }
  }

  dynamic _unwrap(Response<dynamic> response) {
    final body = response.data;

    if (body is Map && body.containsKey('isSuccess')) {
      if (body['isSuccess'] == true) {
        return body['data'];
      }

      throw ApiException(
        body['error']?.toString() ?? 'Не удалось выполнить запрос.',
        type: ApiErrorType.parse(body['errorType']?.toString()),
        statusCode: response.statusCode,
      );
    }

    return body;
  }

  Future<void> _onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    if (options.extra['skipAuth'] != true) {
      final token = await _tokenStorage.readAccessToken();

      if (token != null && token.isNotEmpty) {
        options.headers['Authorization'] = 'Bearer $token';
      }
    }

    handler.next(options);
  }

  Future<void> _onError(
    DioException error,
    ErrorInterceptorHandler handler,
  ) async {
    final options = error.requestOptions;

    final shouldTryRefresh =
        error.response?.statusCode == 401 &&
        options.extra['skipAuth'] != true &&
        options.extra['retried'] != true;

    if (!shouldTryRefresh) {
      handler.next(error);
      return;
    }

    final sentHeader = options.headers['Authorization'];
    final current = await _tokenStorage.readAccessToken();
    final alreadyRefreshed = current != null && sentHeader != 'Bearer $current';

    if (!alreadyRefreshed) {
      final outcome = await _refreshTokens();

      if (outcome == _RefreshOutcome.rejected) {
        await _tokenStorage.clear();
        _sessionEvents.notifyExpired();
        handler.next(error);
        return;
      }

      if (outcome == _RefreshOutcome.failed) {
        handler.next(error);
        return;
      }
    }

    try {
      options.extra['retried'] = true;
      final token = await _tokenStorage.readAccessToken();
      options.headers['Authorization'] = 'Bearer $token';

      final response = await dio.fetch<dynamic>(options);
      handler.resolve(response);
    } on DioException catch (retryError) {
      handler.next(retryError);
    }
  }

  Future<_RefreshOutcome> _refreshTokens() {
    return _refreshing ??= _doRefresh().whenComplete(() => _refreshing = null);
  }

  Future<_RefreshOutcome> _doRefresh() async {
    final refreshToken = await _tokenStorage.readRefreshToken();

    if (refreshToken == null || refreshToken.isEmpty) {
      return _RefreshOutcome.rejected;
    }

    try {
      final response = await _refreshDio.post<dynamic>(
        '/api/auth/refresh',
        data: {'refreshToken': refreshToken},
      );

      final body = response.data;

      if (body is Map && body['isSuccess'] == true && body['data'] is Map) {
        final data = body['data'] as Map;

        await _tokenStorage.saveTokens(
          access: data['accessToken'] as String,
          refresh: data['refreshToken'] as String,
        );

        return _RefreshOutcome.success;
      }

      return _RefreshOutcome.rejected;
    } on DioException catch (e) {
      final status = e.response?.statusCode;

      if (status == 401 || status == 400 || status == 403) {
        return _RefreshOutcome.rejected;
      }

      return _RefreshOutcome.failed;
    }
  }
}
