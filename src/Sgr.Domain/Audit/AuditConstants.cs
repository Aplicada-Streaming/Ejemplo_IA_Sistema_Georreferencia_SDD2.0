namespace Sgr.Domain.Audit;

public static class AuditEntityType
{
    public const string Survey = "survey";
    public const string Point = "point";
    public const string Photo = "photo";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Survey, Point, Photo };
    public static bool IsValid(string s) => All.Contains(s);
}

public static class AuditEventType
{
    public const string Created = "created";
    public const string FieldUpdated = "field_updated";
    public const string Deleted = "deleted";
    public const string Restored = "restored";
    public const string Merged = "merged";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string> { Created, FieldUpdated, Deleted, Restored, Merged };
    public static bool IsValid(string s) => All.Contains(s);
}

public static class AuditOrigin
{
    public const string MobileCapture = "mobile_capture";
    public const string MobileEdit = "mobile_edit";
    public const string WebEdit = "web_edit";
    public const string WebManualUpload = "web_manual_upload";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string> { MobileCapture, MobileEdit, WebEdit, WebManualUpload };
    public static bool IsValid(string s) => All.Contains(s);
}
