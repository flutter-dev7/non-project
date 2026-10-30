class PagedResult<T> {
  const PagedResult({required this.items, required this.hasMore});

  final List<T> items;
  final bool hasMore;

  factory PagedResult.fromJson(
    dynamic json,
    T Function(Map<String, dynamic> json) parse, {
    required int page,
    required int pageSize,
  }) {
    List<dynamic> raw;
    var total = 0;

    if (json is List) {
      raw = json;
    } else {
      final map = json as Map<String, dynamic>;
      raw = (map['items'] ?? map['data'] ?? map['results'] ?? const <dynamic>[]) as List;
      total = _int(map['totalCount'] ?? map['total'] ?? map['totalItems']);
    }

    final items = raw.map((e) => parse(e as Map<String, dynamic>)).toList();
    final hasMore = total > 0 ? page * pageSize < total : items.length >= pageSize;

    return PagedResult(items: items, hasMore: hasMore);
  }

  static int _int(dynamic value) => value is num ? value.toInt() : 0;
}