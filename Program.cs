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
    private const string PipePrefix = "MultipleFileHelper_Pipe_";

    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        ToolAction action = ToolAction.Numbering;
        var directFiles = new List<string>();

        foreach (var arg in args)
        {
            if (string.Equals(arg, "--action=remove-prefix", StringComparison.OrdinalIgnoreCase))
            {
                action = ToolAction.RemovePrefixChars;
            }
            else if (string.Equals(arg, "--action=numbering", StringComparison.OrdinalIgnoreCase))
            {
                action = ToolAction.Numbering;
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

        string mutexName = @"Global\MultipleFileHelper_Mutex_" + action.ToString();
        string pipeName = PipePrefix + action.ToString();

        bool isPrimary;
        using var mutex = new Mutex(true, mutexName, out isPrimary);

        if (!isPrimary)
        {
            SendFilesToPrimaryInstance(pipeName, directFiles);
            return;
        }

        var allFiles = new List<string>(directFiles);
        CollectFilesFromSecondaryInstances(pipeName, allFiles, timeoutMs: 400);

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

        using var form = new MainForm(allFiles, action);
        if (form.ShowDialog() != DialogResult.OK)
            return;

        var sortedFiles = form.GetSortedFiles();
        var plans = sortedFiles.Select((file, index) =>
        {
            string directory = Path.GetDirectoryName(file)!;
            string newFileName = form.BuildNewFileName(file, index);
            return new RenamePlan(file, Path.Combine(directory, newFileName));
        }).ToList();

        RenameFiles(plans);
    }

    private static void SendFilesToPrimaryInstance(string pipeName, List<string> files)
    {
        if (files.Count == 0) return;
        try
        {
            using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
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

    private static void CollectFilesFromSecondaryInstances(string pipeName, List<string> fileList, int timeoutMs)
    {
        using var cts = new CancellationTokenSource(timeoutMs);

        while (!cts.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    pipeName,
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
                if (string.Equals(plan.OldPath, plan.NewPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                string dir = Path.GetDirectoryName(plan.OldPath)!;
                string temp = Path.Combine(dir, ".fn_" + Guid.NewGuid().ToString("N") + ".tmp");
                File.Move(plan.OldPath, temp);
                tempPaths.Add(plan.OldPath, temp);
            }

            foreach (var plan in plans)
            {
                if (string.Equals(plan.OldPath, plan.NewPath, StringComparison.OrdinalIgnoreCase))
                    continue;

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
