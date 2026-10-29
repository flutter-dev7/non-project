import 'user_role.dart';

class AuthUser {
  const AuthUser({
    required this.id,
    required this.firstName,
    required this.lastName,
    required this.fullName,
    required this.phone,
    required this.email,
    required this.role,
  });

  final String id;
  final String firstName;
  final String lastName;
  final String fullName;
  final String phone;
  final String? email;
  final UserRole role;
}