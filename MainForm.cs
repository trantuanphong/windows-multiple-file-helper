using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MultipleFileHelper;

public class MainForm : Form
{
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int StrCmpLogicalW(string psz1, string psz2);

    private readonly List<string> _rawFiles;
    public BatchRenameConfig Config { get; private set; } = new();

    private TabControl tabFeatures = null!;
    private ComboBox cboSort = null!;

    // Tab 1: Đánh số
    private NumericUpDown numStart = null!;
    private NumericUpDown numStep = null!;
    private NumericUpDown numPadding = null!;
    private TextBox txtPrefix = null!;
    private TextBox txtSuffix = null!;
    private TextBox txtSeparator = null!;
    private ComboBox cboPosition = null!;
    private CheckBox chkKeepName = null!;

    // Tab 2: Xóa N ký tự đầu
    private NumericUpDown numRemoveCount = null!;
    private CheckBox chkTrimLeading = null!;

    // Preview
    private ListView lvPreview = null!;
    private Label lblCount = null!;
    private Button btnApply = null!;
    private Button btnCancel = null!;

    public MainForm(List<string> files, ToolAction initialAction = ToolAction.Numbering)
    {
        _rawFiles = files;
        Config.Action = initialAction;
        InitializeComponent();
        if (initialAction == ToolAction.RemovePrefixChars)
        {
            tabFeatures.SelectedIndex = 1;
        }
        UpdatePreview();
    }

    private void InitializeComponent()
    {
        Text = "MultipleFileHelper - Tiện ích xử lý nhiều file";
        Width = 920;
        Height = 680;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        MinimumSize = new Size(800, 550);

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(12)
        };
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Top bar: Tabs + Sort
        var pnlTop = new Panel { Dock = DockStyle.Top, AutoSize = true };

        tabFeatures = new TabControl { Dock = DockStyle.Top, Height = 145 };

        // TAB 1: Đánh số thứ tự
        var tabNumbering = new TabPage("1. Đánh số thứ tự");
        var flowNum = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(8)
        };

        flowNum.Controls.Add(CreateLabel("Bắt đầu:"));
        numStart = new NumericUpDown { Minimum = 0, Maximum = 999999, Value = 1, Width = 70 };
        numStart.ValueChanged += (s, e) => UpdatePreview();
        flowNum.Controls.Add(numStart);

        flowNum.Controls.Add(CreateLabel("Bước:"));
        numStep = new NumericUpDown { Minimum = 1, Maximum = 1000, Value = 1, Width = 60 };
        numStep.ValueChanged += (s, e) => UpdatePreview();
        flowNum.Controls.Add(numStep);

        flowNum.Controls.Add(CreateLabel("Padding 0:"));
        numPadding = new NumericUpDown { Minimum = 1, Maximum = 10, Value = 3, Width = 60 };
        numPadding.ValueChanged += (s, e) => UpdatePreview();
        flowNum.Controls.Add(numPadding);

        flowNum.Controls.Add(CreateLabel("Ký tự nối:"));
        txtSeparator = new TextBox { Text = "_", Width = 55 };
        txtSeparator.TextChanged += (s, e) => UpdatePreview();
        flowNum.Controls.Add(txtSeparator);

        flowNum.Controls.Add(CreateLabel("Tiền tố:"));
        txtPrefix = new TextBox { Width = 85 };
        txtPrefix.TextChanged += (s, e) => UpdatePreview();
        flowNum.Controls.Add(txtPrefix);

        flowNum.Controls.Add(CreateLabel("Hậu tố:"));
        txtSuffix = new TextBox { Width = 85 };
        txtSuffix.TextChanged += (s, e) => UpdatePreview();
        flowNum.Controls.Add(txtSuffix);

        flowNum.Controls.Add(CreateLabel("Vị trí:"));
        cboPosition = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
        cboPosition.Items.AddRange(new object[] { "Đầu tên", "Cuối tên" });
        cboPosition.SelectedIndex = 0;
        cboPosition.SelectedIndexChanged += (s, e) => UpdatePreview();
        flowNum.Controls.Add(cboPosition);

        chkKeepName = new CheckBox { Text = "Giữ tên gốc", Checked = true, AutoSize = true, Margin = new Padding(10, 6, 0, 0) };
        chkKeepName.CheckedChanged += (s, e) => UpdatePreview();
        flowNum.Controls.Add(chkKeepName);

        tabNumbering.Controls.Add(flowNum);
        tabFeatures.TabPages.Add(tabNumbering);

        // TAB 2: Xóa n ký tự đầu
        var tabRemove = new TabPage("2. Xóa n ký tự đầu");
        var flowRemove = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(12)
        };

        flowRemove.Controls.Add(CreateLabel("Số ký tự cần cắt bỏ ở đầu tên:"));
        numRemoveCount = new NumericUpDown { Minimum = 1, Maximum = 500, Value = 1, Width = 80 };
        numRemoveCount.ValueChanged += (s, e) => UpdatePreview();
        flowRemove.Controls.Add(numRemoveCount);

        chkTrimLeading = new CheckBox { Text = "Tự động xóa khoảng trắng / ký tự nối thừa sau khi cắt (vd: _, -, space)", Checked = true, AutoSize = true, Margin = new Padding(15, 6, 0, 0) };
        chkTrimLeading.CheckedChanged += (s, e) => UpdatePreview();
        flowRemove.Controls.Add(chkTrimLeading);

        tabRemove.Controls.Add(flowRemove);
        tabFeatures.TabPages.Add(tabRemove);

        tabFeatures.SelectedIndexChanged += (s, e) =>
        {
            Config.Action = tabFeatures.SelectedIndex == 1 ? ToolAction.RemovePrefixChars : ToolAction.Numbering;
            UpdatePreview();
        };

        // Sort Bar
        var pnlSort = new Panel { Dock = DockStyle.Bottom, Height = 35, Padding = new Padding(4, 4, 0, 0) };
        var lblSort = CreateLabel("Thứ tự sắp xếp:");
        lblSort.Location = new Point(4, 7);
        cboSort = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180, Location = new Point(115, 4) };
        cboSort.Items.AddRange(new object[] {
            "Theo Explorer",
            "Tên A → Z (tự nhiên)",
            "Tên Z → A (tự nhiên)",
            "Ngày tạo cũ → mới",
            "Ngày tạo mới → cũ",
            "Sửa đổi cũ → mới",
            "Sửa đổi mới → cũ"
        });
        cboSort.SelectedIndex = 1;
        cboSort.SelectedIndexChanged += (s, e) => UpdatePreview();

        pnlSort.Controls.Add(lblSort);
        pnlSort.Controls.Add(cboSort);

        pnlTop.Controls.Add(tabFeatures);
        pnlTop.Controls.Add(pnlSort);
        table.Controls.Add(pnlTop, 0, 0);

        // Preview table
        var grpPreview = new GroupBox
        {
            Text = "Xem trước danh sách (Live Preview)",
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };

        lvPreview = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        lvPreview.Columns.Add("STT", 50);
        lvPreview.Columns.Add("Tên gốc", 380);
        lvPreview.Columns.Add("Tên mới sau khi xử lý", 430);

        grpPreview.Controls.Add(lvPreview);
        table.Controls.Add(grpPreview, 0, 1);

        // Bottom action bar
        var pnlBottom = new Panel { Dock = DockStyle.Fill, Height = 45 };

        lblCount = new Label
        {
            AutoSize = true,
            Location = new Point(0, 12),
            Text = $"Đã chọn: {_rawFiles.Count} file"
        };
        pnlBottom.Controls.Add(lblCount);

        btnApply = new Button
        {
            Text = "Bắt đầu đổi tên",
            DialogResult = DialogResult.OK,
            Width = 140,
            Height = 32,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlBottom.Width - 250, 6),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnApply.Click += (s, e) =>
        {
            ApplyConfigValues();
            Close();
        };

        btnCancel = new Button
        {
            Text = "Hủy",
            DialogResult = DialogResult.Cancel,
            Width = 90,
            Height = 32,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlBottom.Width - 100, 6)
        };
        btnCancel.Click += (s, e) => Close();

        pnlBottom.Resize += (s, e) =>
        {
            btnCancel.Location = new Point(pnlBottom.Width - 100, 6);
            btnApply.Location = new Point(pnlBottom.Width - 250, 6);
        };

        pnlBottom.Controls.Add(btnApply);
        pnlBottom.Controls.Add(btnCancel);
        table.Controls.Add(pnlBottom, 0, 2);

        Controls.Add(table);
        AcceptButton = btnApply;
        CancelButton = btnCancel;
    }

    private static Label CreateLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Margin = new Padding(6, 7, 3, 0)
        };
    }

    private void ApplyConfigValues()
    {
        Config.Action = tabFeatures.SelectedIndex == 1 ? ToolAction.RemovePrefixChars : ToolAction.Numbering;
        Config.StartNumber = (int)numStart.Value;
        Config.Step = (int)numStep.Value;
        Config.PaddingZeros = (int)numPadding.Value;
        Config.Prefix = txtPrefix.Text;
        Config.Suffix = txtSuffix.Text;
        Config.Separator = txtSeparator.Text;
        Config.Position = cboPosition.SelectedIndex == 1 ? NumberPosition.Suffix : NumberPosition.Prefix;
        Config.KeepOriginalName = chkKeepName.Checked;
        Config.RemoveCharCount = (int)numRemoveCount.Value;
        Config.TrimLeadingWhitespace = chkTrimLeading.Checked;
        Config.SortOrder = (SortOrderType)cboSort.SelectedIndex;
    }

    public List<string> GetSortedFiles()
    {
        var list = new List<string>(_rawFiles);
        switch ((SortOrderType)cboSort.SelectedIndex)
        {
            case SortOrderType.NameAsc:
                list.Sort((a, b) => StrCmpLogicalW(Path.GetFileName(a), Path.GetFileName(b)));
                break;
            case SortOrderType.NameDesc:
                list.Sort((a, b) => StrCmpLogicalW(Path.GetFileName(b), Path.GetFileName(a)));
                break;
            case SortOrderType.DateCreatedAsc:
                list.Sort((a, b) => File.GetCreationTimeUtc(a).CompareTo(File.GetCreationTimeUtc(b)));
                break;
            case SortOrderType.DateCreatedDesc:
                list.Sort((a, b) => File.GetCreationTimeUtc(b).CompareTo(File.GetCreationTimeUtc(a)));
                break;
            case SortOrderType.DateModifiedAsc:
                list.Sort((a, b) => File.GetLastWriteTimeUtc(a).CompareTo(File.GetLastWriteTimeUtc(b)));
                break;
            case SortOrderType.DateModifiedDesc:
                list.Sort((a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));
                break;
            case SortOrderType.Explorer:
            default:
                break;
        }
        return list;
    }

    public string BuildNewFileName(string filePath, int index)
    {
        string ext = Path.GetExtension(filePath);
        string oldName = Path.GetFileNameWithoutExtension(filePath);

        if (tabFeatures.SelectedIndex == 1) // Remove N chars
        {
            int removeCount = (int)numRemoveCount.Value;
            string newName = oldName.Length <= removeCount ? "" : oldName.Substring(removeCount);

            if (chkTrimLeading.Checked)
            {
                newName = newName.TrimStart(' ', '-', '_', '.');
            }

            if (string.IsNullOrEmpty(newName))
            {
                newName = oldName; // fallback
            }

            return $"{newName}{ext}";
        }

        // Action: Numbering
        int start = (int)numStart.Value;
        int step = (int)numStep.Value;
        int padding = (int)numPadding.Value;
        string prefix = txtPrefix.Text;
        string suffix = txtSuffix.Text;
        string sep = txtSeparator.Text;
        bool keepName = chkKeepName.Checked;
        bool isNumberPrefix = cboPosition.SelectedIndex == 0;

        long numVal = start + (long)index * step;
        string numStr = numVal.ToString().PadLeft(padding, '0');

        string core;
        if (!keepName)
        {
            core = numStr;
        }
        else
        {
            if (isNumberPrefix)
            {
                core = string.IsNullOrEmpty(sep) ? $"{numStr}{oldName}" : $"{numStr}{sep}{oldName}";
            }
            else
            {
                core = string.IsNullOrEmpty(sep) ? $"{oldName}{numStr}" : $"{oldName}{sep}{numStr}";
            }
        }

        return $"{prefix}{core}{suffix}{ext}";
    }

    private void UpdatePreview()
    {
        lvPreview.BeginUpdate();
        lvPreview.Items.Clear();

        var sorted = GetSortedFiles();
        for (int i = 0; i < sorted.Count; i++)
        {
            string oldPath = sorted[i];
            string oldName = Path.GetFileName(oldPath);
            string newName = BuildNewFileName(oldPath, i);

            var item = new ListViewItem((i + 1).ToString());
            item.SubItems.Add(oldName);
            item.SubItems.Add(newName);
            lvPreview.Items.Add(item);
        }

        lvPreview.EndUpdate();
    }
}
