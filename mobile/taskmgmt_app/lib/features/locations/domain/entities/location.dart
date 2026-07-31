class Location {
  const Location({
    required this.id,
    required this.name,
    required this.address,
    required this.latitude,
    required this.longitude,
    required this.parentLocationId,
  });

  final String id;
  final String name;
  final String? address;
  final double latitude;
  final double longitude;
  final String? parentLocationId;
}
