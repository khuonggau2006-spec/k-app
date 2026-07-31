import '../../domain/entities/location.dart';

class LocationModel {
  const LocationModel({
    required this.id,
    required this.name,
    required this.address,
    required this.latitude,
    required this.longitude,
    required this.isActive,
    required this.parentLocationId,
  });

  final String id;
  final String name;
  final String? address;
  final double latitude;
  final double longitude;
  final bool isActive;
  final String? parentLocationId;

  factory LocationModel.fromJson(Map<String, dynamic> json) => LocationModel(
        id: json['id'] as String,
        name: json['name'] as String,
        address: json['address'] as String?,
        latitude: (json['latitude'] as num).toDouble(),
        longitude: (json['longitude'] as num).toDouble(),
        isActive: json['isActive'] as bool,
        parentLocationId: json['parentLocationId'] as String?,
      );

  Location toDomain() => Location(
        id: id,
        name: name,
        address: address,
        latitude: latitude,
        longitude: longitude,
        parentLocationId: parentLocationId,
      );
}
