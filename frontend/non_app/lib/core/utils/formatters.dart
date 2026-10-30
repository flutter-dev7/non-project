import 'package:intl/intl.dart';

class Formatters {
  Formatters._();

  static final NumberFormat _number = NumberFormat('#,##0.##', 'ru');
  static final DateFormat _date = DateFormat('d MMMM y', 'ru');
  static final DateFormat _shortDate = DateFormat('d MMM', 'ru');
  static final DateFormat _dateTime = DateFormat('d MMM, HH:mm', 'ru');
  static final DateFormat _api = DateFormat('yyyy-MM-dd');

  static String money(num value) => '${_number.format(value)} сом.';

  static String date(DateTime value) => _date.format(value);

  static String shortDate(DateTime value) => _shortDate.format(value);

  static String dateTime(DateTime value) => _dateTime.format(value);

  static String apiDate(DateTime value) => _api.format(value);

  static DateTime parseDate(String value) {
    final parsed = DateTime.parse(value);

    return DateTime(parsed.year, parsed.month, parsed.day);
  }

  /// Сервер отдаёт UTC; показываем время Таджикистана (UTC+5).
  static DateTime parseDateTime(String value) {
    final hasZone =
        value.endsWith('Z') || RegExp(r'[+-]\d\d:\d\d$').hasMatch(value);
    final parsed = DateTime.parse(hasZone ? value : '${value}Z');

    return parsed.toUtc().add(const Duration(hours: 5));
  }

  static DateTime nowTj() =>
      DateTime.now().toUtc().add(const Duration(hours: 5));

  static DateTime today() {
    final now = nowTj();

    return DateTime(now.year, now.month, now.day);
  }

  static final DateFormat _time = DateFormat('HH:mm');

  static String time(DateTime value) => _time.format(value);
}
