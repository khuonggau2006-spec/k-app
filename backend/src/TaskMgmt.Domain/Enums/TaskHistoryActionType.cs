namespace TaskMgmt.Domain.Enums;

public enum TaskHistoryActionType
{
    Created = 0,
    FieldChanged = 1,
    StatusChanged = 2,
    Deleted = 3,
    AssigneeAdded = 4,
    AssigneeRemoved = 5,
    AssigneeRoleChanged = 6,
    CommentAdded = 7,
    AttachmentAdded = 8,
    AttachmentRemoved = 9,
}
