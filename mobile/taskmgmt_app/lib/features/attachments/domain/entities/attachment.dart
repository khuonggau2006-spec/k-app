class Attachment {
  const Attachment({
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

  bool get isImage => contentType.startsWith('image/');
}
