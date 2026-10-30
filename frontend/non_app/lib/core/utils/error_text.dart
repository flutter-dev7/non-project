import '../network/api_exception.dart';

String errorText(Object error) {
  if (error is ApiException) return error.message;

  return 'Что-то пошло не так. Попробуйте ещё раз.';
}