enum PaymentMethod {
  cash,
  transfer;

  String get apiName => this == PaymentMethod.transfer ? 'Transfer' : 'Cash';

  String get label => this == PaymentMethod.transfer ? 'Перевод' : 'Наличные';
}