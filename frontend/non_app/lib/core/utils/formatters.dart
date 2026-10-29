import 'package:intl/intl.dart';

class Formatters {
  Formatters._();

  static final NumberFormat _money = NumberFormat('#,##0.##', 'ru');
  static final DateFormat _date = DateFormat('dd.MM.yyyy');
  static final DateFormat _dateTime = DateFormat('dd.MM.yyyy HH:mm');
  static final DateFormat _apiDate = DateFormat('yyyy-MM-dd');

  static String money(num value) => '${_money.format(value)} сом.';

  static String date(DateTime value) => _date.format(value);

  static String dateTime(DateTime value) => _dateTime.format(value.toLocal());

  static String apiDate(DateTime value) => _apiDate.format(value);

  static DateTime? parseDate(String? value) =>
      value == null ? null : DateTime.tryParse(value);

  static DateTime? parseDateTime(String? value) {
    if (value == null) return null;

    final parsed = DateTime.tryParse(value);
    if (parsed == null) return null;

    return parsed.isUtc ? parsed.toLocal() : parsed;
  }
}