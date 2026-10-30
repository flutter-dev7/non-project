class Visit {
  const Visit({
    required this.id,
    required this.shopId,
    required this.shopName,
    required this.shopAddress,
    required this.agentName,
    required this.visitedAt,
    required this.note,
  });

  final String id;
  final String shopId;
  final String shopName;
  final String shopAddress;
  final String? agentName;
  final DateTime visitedAt;
  final String? note;
}