namespace TaskMgmt.Application.Features.TaskHistories.Common;

internal static class TaskHistoryFieldLabels
{
    private static readonly Dictionary<string, string> Labels = new()
    {
        ["Title"] = "tiêu đề",
        ["Description"] = "mô tả",
        ["DueDateUtc"] = "hạn hoàn thành",
        ["LocationId"] = "vị trí",
        ["ParentTaskId"] = "công việc cha",
    };

    public static string GetLabel(string fieldName) => Labels.GetValueOrDefault(fieldName, fieldName);
}
