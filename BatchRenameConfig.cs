namespace MultipleFileHelper;

public enum ToolAction
{
    Numbering,
    RemovePrefixChars
}

public class BatchRenameConfig
{
    public ToolAction Action { get; set; } = ToolAction.Numbering;

    // Cấu hình Đánh số
    public int StartNumber { get; set; } = 1;
    public int Step { get; set; } = 1;
    public int PaddingZeros { get; set; } = 3;
    public string Prefix { get; set; } = "";
    public string Suffix { get; set; } = "";
    public string Separator { get; set; } = "_";
    public NumberPosition Position { get; set; } = NumberPosition.Prefix;
    public bool KeepOriginalName { get; set; } = true;

    // Cấu hình Xóa N ký tự đầu
    public int RemoveCharCount { get; set; } = 1;
    public bool TrimLeadingWhitespace { get; set; } = true;

    // Sắp xếp
    public SortOrderType SortOrder { get; set; } = SortOrderType.Explorer;
}

public enum NumberPosition
{
    Prefix,
    Suffix
}

public enum SortOrderType
{
    Explorer,
    NameAsc,
    NameDesc,
    DateCreatedAsc,
    DateCreatedDesc,
    DateModifiedAsc,
    DateModifiedDesc
}
