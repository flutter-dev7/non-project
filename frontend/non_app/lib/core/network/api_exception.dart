import 'package:dio/dio.dart';

enum ApiErrorType {
  validation,
  notFound,
  conflict,
  unauthorized,
  forbidden,
  tooManyRequests,
  failure,
  network,
  unknown;

  static ApiErrorType parse(String? value) {
    switch (value?.toLowerCase()) {
      case 'validation':
        return ApiErrorType.validation;
      case 'notfound':
        return ApiErrorType.notFound;
      case 'conflict':
        return ApiErrorType.conflict;
      case 'unauthorized':
        return ApiErrorType.unauthorized;
      case 'forbidden':
        return ApiErrorType.forbidden;
      case 'toomanyrequests':
        return ApiErrorType.tooManyRequests;
      case 'failure':
        return ApiErrorType.failure;
      default:
        return ApiErrorType.unknown;
    }
  }

  static ApiErrorType fromStatus(int? status) {
    if (status == null) return ApiErrorType.unknown;
    if (status == 400) return ApiErrorType.validation;
    if (status == 401) return ApiErrorType.unauthorized;
    if (status == 403) return ApiErrorType.forbidden;
    if (status == 404) return ApiErrorType.notFound;
    if (status == 409) return ApiErrorType.conflict;
    if (status == 429) return ApiErrorType.tooManyRequests;
    if (status >= 500) return ApiErrorType.failure;
    return ApiErrorType.unknown;
  }
}

class ApiException implements Exception {
  const ApiException(
    this.message, {
    this.type = ApiErrorType.unknown,
    this.statusCode,
  });

  final String message;
  final ApiErrorType type;
  final int? statusCode;

  bool get isNetwork => type == ApiErrorType.network;

  factory ApiException.fromDio(DioException e) {
    switch (e.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
      case DioExceptionType.connectionError:
        return const ApiException(
          'Нет связи с сервером. Проверьте интернет и попробуйте снова.',
          type: ApiErrorType.network,
        );
      case DioExceptionType.cancel:
        return const ApiException('Запрос отменён.');
      default:
        break;
    }

    final status = e.response?.statusCode;
    final body = e.response?.data;

    if (body is Map) {
      final message = body['error'];

      if (message is String && message.isNotEmpty) {
        final parsed = ApiErrorType.parse(body['errorType']?.toString());

        return ApiException(
          message,
          type: parsed == ApiErrorType.unknown ? ApiErrorType.fromStatus(status) : parsed,
          statusCode: status,
        );
      }
    }

    return ApiException(
      _defaultMessage(status),
      type: ApiErrorType.fromStatus(status),
      statusCode: status,
    );
  }

  static String _defaultMessage(int? status) {
    if (status == 401) return 'Требуется вход в систему.';
    if (status == 403) return 'Недостаточно прав.';
    if (status == 404) return 'Не найдено.';
    if (status == 429) return 'Слишком много запросов. Подождите немного.';
    if (status != null && status >= 500) return 'Ошибка сервера. Попробуйте позже.';
    return 'Не удалось выполнить запрос.';
  }

  @override
  String toString() => message;
}