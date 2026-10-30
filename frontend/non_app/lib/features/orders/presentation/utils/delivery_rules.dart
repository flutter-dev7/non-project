import '../../../../core/utils/formatters.dart';

class DeliveryRules {
  DeliveryRules._();

  static const int maxDays = 30;

  /// Самая ранняя дата доставки. После времени приёма заказов на завтра
  /// (например 18:00) — послезавтра. Окончательно проверяет сервер.
  static DateTime minDate(String? cutoff) {
    final now = Formatters.nowTj();
    var days = 1;

    final parts = cutoff?.split(':');

    if (parts != null && parts.length == 2) {
      final hour = int.tryParse(parts[0]);
      final minute = int.tryParse(parts[1]);

      if (hour != null && minute != null && now.hour * 60 + now.minute >= hour * 60 + minute) {
        days = 2;
      }
    }

    return DateTime(now.year, now.month, now.day + days);
  }

  static DateTime maxDate() {
    final today = Formatters.today();

    return DateTime(today.year, today.month, today.day + maxDays);
  }
}