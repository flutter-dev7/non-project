import '../../domain/entities/shop.dart';

class ShopModel extends Shop {
  const ShopModel({
    required super.id,
    required super.name,
    required super.address,
    required super.phone,
    required super.contactName,
    required super.debtLimit,
    required super.isActive,
  });

  factory ShopModel.fromJson(Map<String, dynamic> json) {
    return ShopModel(
      id: json['id'] as String,
      name: json['name'] as String? ?? '',
      address: json['address'] as String? ?? '',
      phone: json['phone'] as String? ?? '',
      contactName: json['contactName'] as String?,
      debtLimit: (json['debtLimit'] as num?)?.toDouble(),
      isActive: json['isActive'] as bool? ?? true,
    );
  }
}