import 'package:flutter_test/flutter_test.dart';
import 'package:non_app/core/utils/validators.dart';

void main() {
  group('Validators.phoneDigits', () {
    test('keeps a 9 digit number', () {
      expect(Validators.phoneDigits('900 00 00 01'), '900000001');
    });

    test('strips the country code', () {
      expect(Validators.phoneDigits('+992900000001'), '900000001');
    });

    test('strips a leading zero', () {
      expect(Validators.phoneDigits('0900000001'), '900000001');
    });

    test('builds the api phone', () {
      expect(Validators.toApiPhone('900 00 00 01'), '+992900000001');
    });
  });

  group('Validators.password', () {
    test('accepts letters and digits', () {
      expect(Validators.password('Secret123'), isNull);
      expect(Validators.password('пароль2026'), isNull);
    });

    test('rejects weak passwords', () {
      expect(Validators.password('short1'), isNotNull);
      expect(Validators.password('onlyletters'), isNotNull);
      expect(Validators.password('12345678'), isNotNull);
    });
  });

  group('Validators.email', () {
    test('accepts a valid email', () {
      expect(Validators.email('user@example.com'), isNull);
    });

    test('rejects invalid emails', () {
      expect(Validators.email(''), isNotNull);
      expect(Validators.email('plain'), isNotNull);
      expect(Validators.email('a@b'), isNotNull);
    });
  });

  group('Validators.code', () {
    test('requires exactly 6 characters', () {
      expect(Validators.code('123456'), isNull);
      expect(Validators.code('12345'), isNotNull);
    });
  });
}