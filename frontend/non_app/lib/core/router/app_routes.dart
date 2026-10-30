class AppRoutes {
  AppRoutes._();

  static const String splash = '/';
  static const String login = '/login';
  static const String register = '/register';
  static const String verifyEmail = '/verify-email';
  static const String forgotPassword = '/forgot-password';
  static const String resetCode = '/reset-code';
  static const String resetPassword = '/reset-password';
  static const String home = '/home';
  static const String changePassword = '/change-password';
  static const String orderDetails = '/orders/:id';
  static const String orderForm = '/order-form';
  static const String balance = '/balance';
  static const String shopDetails = '/shop-details';
  static const String shopForm = '/shop-form';

  static String orderDetailsFor(String id) => '/orders/$id';

  static const Set<String> publicRoutes = {
    login,
    register,
    verifyEmail,
    forgotPassword,
    resetCode,
    resetPassword,
  };

  static String verifyEmailFor(String email, {bool send = false}) => Uri(
    path: verifyEmail,
    queryParameters: {'email': email, if (send) 'send': '1'},
  ).toString();

  static String resetCodeFor(String email) =>
      Uri(path: resetCode, queryParameters: {'email': email}).toString();
}
