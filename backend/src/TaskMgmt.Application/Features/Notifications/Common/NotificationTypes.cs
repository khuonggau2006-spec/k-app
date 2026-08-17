namespace TaskMgmt.Application.Features.Notifications.Common;

// Nguồn sự thật duy nhất cho các giá trị hợp lệ của Notification.Type - dùng để validate PUT
// preferences và để GET preferences trả đủ 10 dòng kể cả loại chưa có row nào (mặc định bật).
// Thêm loại thông báo mới ở NotifyAsync call site nào cũng PHẢI cập nhật danh sách này, nếu
// không loại đó sẽ không xuất hiện trong màn cài đặt (coi như luôn bật, không tắt được).
public static class NotificationTypes
{
    public static readonly IReadOnlyList<string> All =
    [
        "FieldChanged",
        "StatusChanged",
        "Deleted",
        "AssigneeAdded",
        "AssigneeRemoved",
        "AssigneeRoleChanged",
        "CommentAdded",
        "AttachmentAdded",
        "DueSoon",
        "Overdue",
    ];
}
