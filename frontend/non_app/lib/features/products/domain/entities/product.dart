class Product {
  const Product({
    required this.id,
    required this.name,
    required this.weightInGrams,
    required this.price,
    required this.imageUrl,
    required this.isActive,
  });

  final String id;
  final String name;
  final int? weightInGrams;
  final double price;
  final String? imageUrl;
  final bool isActive;
}