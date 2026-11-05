namespace MultipleFileHelper;

public class FileItem
{
    public string OriginalPath { get; set; }
    public string WorkingName { get; set; } // Tên hiện hành trong Helper (không gồm đuôi)
    public string Extension { get; set; }
    public string DirectoryPath { get; set; }

    public FileItem(string originalPath)
    {
        OriginalPath = originalPath;
        DirectoryPath = System.IO.Path.GetDirectoryName(originalPath) ?? "";
        Extension = System.IO.Path.GetExtension(originalPath);
        WorkingName = System.IO.Path.GetFileNameWithoutExtension(originalPath);
    }

    public string OriginalFileName => System.IO.Path.GetFileName(OriginalPath);
    public string FinalFileName => WorkingName + Extension;
    public string FinalPath => System.IO.Path.Combine(DirectoryPath, FinalFileName);
}
