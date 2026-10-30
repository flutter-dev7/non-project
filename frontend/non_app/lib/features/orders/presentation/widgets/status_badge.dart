import 'package:flutter/material.dart';

import '../../domain/entities/order_status.dart';

class StatusBadge extends StatelessWidget {
  const StatusBadge({super.key, required this.status});

  final OrderStatus status;

  static Color colorOf(OrderStatus status) => switch (status) {
        OrderStatus.created => const Color(0xFF2F6FDE),
        OrderStatus.accepted => const Color(0xFF7B4FD6),
        OrderStatus.baking => const Color(0xFFD9822B),
        OrderStatus.onTheWay => const Color(0xFF1F9AA8),
        OrderStatus.delivered => const Color(0xFF2E9B5A),
        OrderStatus.cancelled => const Color(0xFFC0392B),
      };

  @override
  Widget build(BuildContext context) {
    final color = colorOf(status);

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(999),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 8,
            height: 8,
            decoration: BoxDecoration(color: color, shape: BoxShape.circle),
          ),
          const SizedBox(width: 6),
          Text(
            status.label,
            style: TextStyle(color: color, fontWeight: FontWeight.w800, fontSize: 13),
          ),
        ],
      ),
    );
  }
}