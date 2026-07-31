import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../../core/network/api_exception.dart';
import '../../../locations/presentation/providers/location_provider.dart';
import '../../domain/entities/work_task.dart';
import '../providers/work_task_provider.dart';

Future<void> showWorkTaskFormSheet(BuildContext context, {WorkTask? task, String? parentTaskId}) {
  return showModalBottomSheet(
    context: context,
    isScrollControlled: true,
    useSafeArea: true,
    builder: (context) => WorkTaskFormSheet(task: task, parentTaskId: parentTaskId),
  );
}

class WorkTaskFormSheet extends ConsumerStatefulWidget {
  const WorkTaskFormSheet({super.key, this.task, this.parentTaskId});

  final WorkTask? task;

  /// Chỉ áp dụng khi tạo mới (tạo công việc con của [parentTaskId]).
  final String? parentTaskId;

  @override
  ConsumerState<WorkTaskFormSheet> createState() => _WorkTaskFormSheetState();
}

class _WorkTaskFormSheetState extends ConsumerState<WorkTaskFormSheet> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _titleController;
  late final TextEditingController _descriptionController;
  DateTime? _dueDate;
  String? _locationId;
  WorkTaskStatus _status = WorkTaskStatus.toDo;

  bool _isSubmitting = false;
  String? _errorMessage;

  bool get _isEditing => widget.task != null;

  @override
  void initState() {
    super.initState();
    final task = widget.task;
    _titleController = TextEditingController(text: task?.title ?? '');
    _descriptionController = TextEditingController(text: task?.description ?? '');
    _dueDate = task?.dueDateUtc?.toLocal();
    _locationId = task?.locationId;
    _status = task?.status ?? WorkTaskStatus.toDo;
  }

  @override
  void dispose() {
    _titleController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  Future<void> _pickDueDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _dueDate ?? now,
      firstDate: DateTime(now.year - 1),
      lastDate: DateTime(now.year + 5),
    );
    if (picked != null) setState(() => _dueDate = picked);
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      final controller = ref.read(workTasksProvider.notifier);
      if (_isEditing) {
        await controller.updateTask(
          id: widget.task!.id,
          title: _titleController.text.trim(),
          description: _descriptionController.text.trim().isEmpty ? null : _descriptionController.text.trim(),
          status: _status,
          dueDateUtc: _dueDate?.toUtc(),
          locationId: _locationId,
        );
        ref.invalidate(taskDetailProvider(widget.task!.id));
        if (widget.task!.parentTaskId != null) {
          ref.invalidate(taskChildrenProvider(widget.task!.parentTaskId!));
        }
      } else {
        await controller.createTask(
          title: _titleController.text.trim(),
          description: _descriptionController.text.trim().isEmpty ? null : _descriptionController.text.trim(),
          dueDateUtc: _dueDate?.toUtc(),
          locationId: _locationId,
          parentTaskId: widget.parentTaskId,
        );
        if (widget.parentTaskId != null) {
          ref.invalidate(taskChildrenProvider(widget.parentTaskId!));
        }
      }
      if (mounted) Navigator.of(context).pop();
    } catch (e) {
      setState(() {
        _errorMessage = e is ApiException ? e.allMessages.join('\n') : 'Đã có lỗi xảy ra. Vui lòng thử lại.';
      });
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final locationsAsync = ref.watch(locationsProvider);

    return Padding(
      padding: EdgeInsets.only(
        left: 16,
        right: 16,
        top: 16,
        bottom: MediaQuery.of(context).viewInsets.bottom + 16,
      ),
      child: SingleChildScrollView(
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                _isEditing ? 'Sửa công việc' : 'Tạo công việc mới',
                style: Theme.of(context).textTheme.titleLarge,
              ),
              const SizedBox(height: 16),
              if (_errorMessage != null) ...[
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: Theme.of(context).colorScheme.errorContainer,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(
                    _errorMessage!,
                    style: TextStyle(color: Theme.of(context).colorScheme.onErrorContainer),
                  ),
                ),
                const SizedBox(height: 16),
              ],
              TextFormField(
                controller: _titleController,
                decoration: const InputDecoration(labelText: 'Tiêu đề'),
                textInputAction: TextInputAction.next,
                validator: (value) {
                  if (value == null || value.trim().isEmpty) return 'Vui lòng nhập tiêu đề.';
                  return null;
                },
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _descriptionController,
                decoration: const InputDecoration(labelText: 'Mô tả (không bắt buộc)'),
                minLines: 2,
                maxLines: 4,
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: _pickDueDate,
                      icon: const Icon(Icons.event_outlined),
                      label: Text(_dueDate == null ? 'Chọn hạn hoàn thành' : DateFormat('dd/MM/yyyy').format(_dueDate!)),
                    ),
                  ),
                  if (_dueDate != null)
                    IconButton(
                      icon: const Icon(Icons.clear),
                      tooltip: 'Bỏ hạn hoàn thành',
                      onPressed: () => setState(() => _dueDate = null),
                    ),
                ],
              ),
              const SizedBox(height: 12),
              locationsAsync.when(
                data: (locations) => DropdownButtonFormField<String?>(
                  initialValue: _locationId,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Vị trí (không bắt buộc)'),
                  items: [
                    const DropdownMenuItem(value: null, child: Text('Không chọn')),
                    ...locations.map(
                      (location) => DropdownMenuItem(
                        value: location.id,
                        child: Text(location.name, overflow: TextOverflow.ellipsis),
                      ),
                    ),
                  ],
                  onChanged: (value) => setState(() => _locationId = value),
                ),
                loading: () => const LinearProgressIndicator(),
                error: (_, _) => const Text('Không tải được danh sách vị trí.'),
              ),
              if (_isEditing) ...[
                const SizedBox(height: 12),
                DropdownButtonFormField<WorkTaskStatus>(
                  initialValue: _status,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Trạng thái'),
                  items: WorkTaskStatus.values
                      .map((status) => DropdownMenuItem(value: status, child: Text(workTaskStatusLabel(status))))
                      .toList(),
                  onChanged: (value) {
                    if (value != null) setState(() => _status = value);
                  },
                ),
              ],
              const SizedBox(height: 20),
              FilledButton(
                onPressed: _isSubmitting ? null : _submit,
                child: _isSubmitting
                    ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                    : Text(_isEditing ? 'Lưu' : 'Tạo'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
