import '../../../../core/utils/formatters.dart';
import '../../domain/entities/visit.dart';

class VisitModel extends Visit {
  const VisitModel({
    required super.id,
    required super.shopId,
    required super.shopName,
    required super.shopAddress,
    required super.agentName,
    required super.visitedAt,
    required super.note,
  });

  factory VisitModel.fromJson(Map<String, dynamic> json) {
    return VisitModel(
      id: json['id'] as String,
      shopId: json['shopId'] as String,
      shopName: json['shopName'] as String? ?? '',
      shopAddress: json['shopAddress'] as String? ?? '',
      agentName: json['agentName'] as String?,
      visitedAt: Formatters.parseDateTime(json['visitedAt'] as String),
      note: json['note'] as String?,
    );
  }
}