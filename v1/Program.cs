using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MultipleFileHelper;

internal static class Program
{
    private const string PipeName = "MultipleFileHelper_Unified_Pipe";
    private const string MutexName = @"Global\MultipleFileHelper_Unified_Mutex";

    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        int initialTab = 0;
        bool isCopyNamesOnly = false;
        var directFiles = new List<string>();

        foreach (var arg in args)
        {
            if (string.Equals(arg, "--action=copy-names", StringComparison.OrdinalIgnoreCase))
            {
                isCopyNamesOnly = true;
            }
            else if (string.Equals(arg, "--action=remove-prefix", StringComparison.OrdinalIgnoreCase))
            {
                initialTab = 1;
            }
            else if (string.Equals(arg, "--action=numbering", StringComparison.OrdinalIgnoreCase))
            {
                initialTab = 0;
            }
            else
            {
                string clean = arg.Trim('"', '\'', ' ');
                if (File.Exists(clean))
                {
                    directFiles.Add(clean);
                }
            }
        }

        bool isPrimary;
        using var mutex = new Mutex(true, MutexName, out isPrimary);

        if (!isPrimary)
        {
            SendFilesToPrimaryInstance(directFiles);
            return;
        }

        var allFiles = new List<string>(directFiles);
        CollectFilesFromSecondaryInstances(allFiles, timeoutMs: 400);

        allFiles = allFiles
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (allFiles.Count == 0)
        {
            MessageBox.Show(
                "Vui lòng chọn ít nhất một file trong File Explorer.",
                "MultipleFileHelper",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // Nếu người dùng chọn chức năng chép tên từ context menu -> Copy nhanh và thông báo nhẹ
        if (isCopyNamesOnly)
        {
            var sb = new StringBuilder();
            foreach (var f in allFiles)
            {
                sb.AppendLine(Path.GetFileName(f));
            }

            try
            {
                Clipboard.SetText(sb.ToString());
                MessageBox.Show(
                    $"Đã sao chép {allFiles.Count} tên file vào Clipboard!",
                    "Sao chép tên file",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi sao chép Clipboard: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return;
        }

        using var form = new MainForm(allFiles, initialTab);
        if (form.ShowDialog() != DialogResult.OK)
            return;

        var plans = form.ResultItems
            .Where(item => !string.Equals(item.OriginalPath, item.FinalPath, StringComparison.OrdinalIgnoreCase))
            .Select(item => new RenamePlan(item.OriginalPath, item.FinalPath))
            .ToList();

        if (plans.Count > 0)
        {
            RenameFiles(plans);
        }
    }

    private static void SendFilesToPrimaryInstance(List<string> files)
    {
        if (files.Count == 0) return;
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(1000);
            using var writer = new StreamWriter(client, Encoding.UTF8);
            foreach (var f in files)
            {
                writer.WriteLine(f);
            }
            writer.Flush();
        }
        catch
        {
        }
    }

    private static void CollectFilesFromSecondaryInstances(List<string> fileList, int timeoutMs)
    {
        using var cts = new CancellationTokenSource(timeoutMs);

        while (!cts.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                var connectTask = server.WaitForConnectionAsync(cts.Token);
                connectTask.Wait(cts.Token);

                if (server.IsConnected)
                {
                    using var reader = new StreamReader(server, Encoding.UTF8);
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            fileList.Add(line.Trim());
                    }
                }
            }
            catch
            {
                break;
            }
        }
    }

    static void RenameFiles(List<RenamePlan> plans)
    {
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var plan in plans)
        {
            if (!targets.Add(plan.NewPath))
            {
                MessageBox.Show(
                    $"Tên file đích bị trùng lặp:\n{Path.GetFileName(plan.NewPath)}",
                    "Lỗi trùng tên",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (File.Exists(plan.NewPath) &&
                !string.Equals(plan.OldPath, plan.NewPath, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    $"File đích đã tồn tại sẵn trên ổ đĩa:\n{plan.NewPath}",
                    "Không thể đổi tên",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
        }

        var tempPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var plan in plans)
            {
                string dir = Path.GetDirectoryName(plan.OldPath)!;
                string temp = Path.Combine(dir, ".mfh_" + Guid.NewGuid().ToString("N") + ".tmp");
                File.Move(plan.OldPath, temp);
                tempPaths.Add(plan.OldPath, temp);
            }

            foreach (var plan in plans)
            {
                File.Move(tempPaths[plan.OldPath], plan.NewPath);
            }

            MessageBox.Show(
                $"Đã đổi tên thành công {plans.Count} file!",
                "Hoàn tất",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            foreach (var plan in plans)
            {
                try
                {
                    if (tempPaths.TryGetValue(plan.OldPath, out var temp))
                    {
                        if (File.Exists(plan.NewPath) && !File.Exists(plan.OldPath))
                            File.Move(plan.NewPath, plan.OldPath);
                        else if (File.Exists(temp) && !File.Exists(plan.OldPath))
                            File.Move(temp, plan.OldPath);
                    }
                }
                catch
                {
                }
            }

            MessageBox.Show(
                "Có lỗi khi đổi tên file:\n" + ex.Message +
                "\n\nHãy kiểm tra lại các file trong thư mục.",
                "Lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    public class RenamePlan
    {
        public string OldPath { get; }
        public string NewPath { get; }

        public RenamePlan(string oldPath, string newPath)
        {
            OldPath = oldPath;
            NewPath = newPath;
        }
    }
}
