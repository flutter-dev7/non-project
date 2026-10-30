class Shop {
  const Shop({
    required this.id,
    required this.name,
    required this.address,
    required this.phone,
    required this.contactName,
    required this.debtLimit,
    required this.isActive,
  });

  final String id;
  final String name;
  final String address;
  final String phone;
  final String? contactName;
  final double? debtLimit;
  final bool isActive;
}