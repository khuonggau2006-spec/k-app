import '../../domain/entities/attachment.dart';

class AttachmentModel {
  const AttachmentModel({
    required this.id,
    required this.workTaskId,
    required this.fileName,
    required this.contentType,
    required this.sizeBytes,
    required this.uploadedByUserId,
    required this.uploadedByFullName,
    required this.uploadedByEmail,
    required this.createdAtUtc,
  });

  final String id;
  final String workTaskId;
  final String fileName;
  final String contentType;
  final int sizeBytes;
  final String? uploadedByUserId;
  final String uploadedByFullName;
  final String uploadedByEmail;
  final DateTime createdAtUtc;

  factory AttachmentModel.fromJson(Map<String, dynamic> json) => AttachmentModel(
        id: json['id'] as String,
        workTaskId: json['workTaskId'] as String,
        fileName: json['fileName'] as String,
        contentType: json['contentType'] as String,
        sizeBytes: json['sizeBytes'] as int,
        uploadedByUserId: json['uploadedByUserId'] as String?,
        uploadedByFullName: json['uploadedByFullName'] as String,
        uploadedByEmail: json['uploadedByEmail'] as String,
        createdAtUtc: DateTime.parse(json['createdAtUtc'] as String),
      );

  Attachment toDomain() => Attachment(
        id: id,
        workTaskId: workTaskId,
        fileName: fileName,
        contentType: contentType,
        sizeBytes: sizeBytes,
        uploadedByUserId: uploadedByUserId,
        uploadedByFullName: uploadedByFullName,
        uploadedByEmail: uploadedByEmail,
        createdAtUtc: createdAtUtc,
      );
}
