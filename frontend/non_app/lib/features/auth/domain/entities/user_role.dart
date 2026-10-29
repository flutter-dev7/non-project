enum UserRole {
  admin,
  agent,
  courier,
  shop;

  static UserRole parse(String? value) {
    switch (value?.toLowerCase()) {
      case 'admin':
        return UserRole.admin;
      case 'agent':
        return UserRole.agent;
      case 'courier':
        return UserRole.courier;
      default:
        return UserRole.shop;
    }
  }

  String get apiName => switch (this) {
        UserRole.admin => 'Admin',
        UserRole.agent => 'Agent',
        UserRole.courier => 'Courier',
        UserRole.shop => 'Shop',
      };

  String get label => switch (this) {
        UserRole.admin => 'Администратор',
        UserRole.agent => 'Агент',
        UserRole.courier => 'Курьер',
        UserRole.shop => 'Магазин',
      };
}