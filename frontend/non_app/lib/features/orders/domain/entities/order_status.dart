enum OrderStatus {
  created,
  accepted,
  baking,
  onTheWay,
  delivered,
  cancelled;

  static OrderStatus parse(String? value) {
    switch (value) {
      case 'Accepted':
        return OrderStatus.accepted;
      case 'Baking':
        return OrderStatus.baking;
      case 'OnTheWay':
        return OrderStatus.onTheWay;
      case 'Delivered':
        return OrderStatus.delivered;
      case 'Cancelled':
        return OrderStatus.cancelled;
      default:
        return OrderStatus.created;
    }
  }

  String get apiName => switch (this) {
        OrderStatus.created => 'New',
        OrderStatus.accepted => 'Accepted',
        OrderStatus.baking => 'Baking',
        OrderStatus.onTheWay => 'OnTheWay',
        OrderStatus.delivered => 'Delivered',
        OrderStatus.cancelled => 'Cancelled',
      };

  String get label => switch (this) {
        OrderStatus.created => 'Новый',
        OrderStatus.accepted => 'Принят',
        OrderStatus.baking => 'Выпекается',
        OrderStatus.onTheWay => 'В пути',
        OrderStatus.delivered => 'Доставлен',
        OrderStatus.cancelled => 'Отменён',
      };
}