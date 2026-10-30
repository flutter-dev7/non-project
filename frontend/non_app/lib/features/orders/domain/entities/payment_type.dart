enum PaymentType {
  now,
  debt;

  static PaymentType parse(String? value) {
    return value == 'Debt' ? PaymentType.debt : PaymentType.now;
  }

  String get apiName => this == PaymentType.debt ? 'Debt' : 'Now';

  String get label => this == PaymentType.debt ? 'В долг' : 'Оплата сразу';
}