import '../../domain/entities/auth_user.dart';
import '../../domain/entities/user_role.dart';

class AuthUserModel extends AuthUser {
  const AuthUserModel({
    required super.id,
    required super.firstName,
    required super.lastName,
    required super.fullName,
    required super.phone,
    required super.email,
    required super.role,
  });

  factory AuthUserModel.fromJson(Map<String, dynamic> json) {
    return AuthUserModel(
      id: json['id'] as String,
      firstName: json['firstName'] as String? ?? '',
      lastName: json['lastName'] as String? ?? '',
      fullName: json['fullName'] as String? ?? '',
      phone: json['phone'] as String? ?? '',
      email: json['email'] as String?,
      role: UserRole.parse(json['role'] as String?),
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'firstName': firstName,
        'lastName': lastName,
        'fullName': fullName,
        'phone': phone,
        'email': email,
        'role': role.apiName,
      };
}