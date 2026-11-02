using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace MultipleFileHelper;

public class MainForm : Form
{
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int StrCmpLogicalW(string psz1, string psz2);

    private readonly List<FileItem> _items = new();
    public List<FileItem> ResultItems => _items;

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
    private Button btnApplyNumbering = null!;

    // Tab 2: Xóa ký tự trong khoảng
    private NumericUpDown numRemoveFrom = null!;
    private NumericUpDown numRemoveTo = null!;
    private CheckBox chkTrimLeading = null!;
    private Button btnApplyRemove = null!;

    // Tab 3: Thêm tiền tố / hậu tố
    private TextBox txtAddPrefix = null!;
    private TextBox txtAddSuffix = null!;
    private Button btnApplyAffix = null!;

    // Preview
    private ListView lvPreview = null!;
    private Label lblCount = null!;
    private Button btnMoveUp = null!;
    private Button btnMoveDown = null!;
    private Button btnReset = null!;
    private Button btnCopyNames = null!;
    private Button btnApplyFinal = null!;
    private Button btnCancel = null!;

    public MainForm(List<string> files, int defaultTabIndex = 0)
    {
        foreach (var f in files)
        {
            _items.Add(new FileItem(f));
        }

        InitializeComponent();
        if (defaultTabIndex >= 0 && defaultTabIndex < tabFeatures.TabPages.Count)
        {
            tabFeatures.SelectedIndex = defaultTabIndex;
        }
        UpdateListView();
    }

    private void InitializeComponent()
    {
        Text = "MultipleFileHelper - Xử lý tên hàng loạt";
        Width = 960;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        MinimumSize = new Size(850, 580);

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

        // Top bar
        var pnlTop = new Panel { Dock = DockStyle.Top, AutoSize = true };
        tabFeatures = new TabControl { Dock = DockStyle.Top, Height = 145 };

        // TAB 1: Đánh số thứ tự
        var tabNumbering = new TabPage("1. Đánh số thứ tự");
        var flowNum = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(6)
        };

        flowNum.Controls.Add(CreateLabel("Bắt đầu:"));
        numStart = new NumericUpDown { Minimum = 0, Maximum = 999999, Value = 1, Width = 65 };
        flowNum.Controls.Add(numStart);

        flowNum.Controls.Add(CreateLabel("Bước:"));
        numStep = new NumericUpDown { Minimum = 1, Maximum = 1000, Value = 1, Width = 55 };
        flowNum.Controls.Add(numStep);

        flowNum.Controls.Add(CreateLabel("Padding 0:"));
        numPadding = new NumericUpDown { Minimum = 1, Maximum = 10, Value = 3, Width = 55 };
        flowNum.Controls.Add(numPadding);

        flowNum.Controls.Add(CreateLabel("Ký tự nối:"));
        txtSeparator = new TextBox { Text = "_", Width = 50 };
        flowNum.Controls.Add(txtSeparator);

        flowNum.Controls.Add(CreateLabel("Tiền tố:"));
        txtPrefix = new TextBox { Width = 75 };
        flowNum.Controls.Add(txtPrefix);

        flowNum.Controls.Add(CreateLabel("Hậu tố:"));
        txtSuffix = new TextBox { Width = 75 };
        flowNum.Controls.Add(txtSuffix);

        flowNum.Controls.Add(CreateLabel("Vị trí:"));
        cboPosition = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 95 };
        cboPosition.Items.AddRange(new object[] { "Đầu tên", "Cuối tên" });
        cboPosition.SelectedIndex = 0;
        flowNum.Controls.Add(cboPosition);

        chkKeepName = new CheckBox { Text = "Giữ tên hiện tại", Checked = true, AutoSize = true, Margin = new Padding(8, 6, 0, 0) };
        flowNum.Controls.Add(chkKeepName);

        btnApplyNumbering = new Button
        {
            Text = "Thực hiện đánh số",
            Width = 145,
            Height = 28,
            BackColor = Color.FromArgb(235, 243, 252),
            Margin = new Padding(12, 3, 0, 0)
        };
        btnApplyNumbering.Click += (s, e) => ExecuteNumberingStage();
        flowNum.Controls.Add(btnApplyNumbering);

        tabNumbering.Controls.Add(flowNum);
        tabFeatures.TabPages.Add(tabNumbering);

        // TAB 2: Xóa ký tự trong khoảng
        var tabRemove = new TabPage("2. Xóa ký tự trong khoảng");
        var flowRemove = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(10)
        };

        flowRemove.Controls.Add(CreateLabel("Xóa từ vị trí thứ:"));
        numRemoveFrom = new NumericUpDown { Minimum = 1, Maximum = 500, Value = 1, Width = 65 };
        flowRemove.Controls.Add(numRemoveFrom);

        flowRemove.Controls.Add(CreateLabel("Đến vị trí thứ:"));
        numRemoveTo = new NumericUpDown { Minimum = 1, Maximum = 500, Value = 3, Width = 65 };
        flowRemove.Controls.Add(numRemoveTo);

        chkTrimLeading = new CheckBox { Text = "Tự động xóa khoảng trắng / ký tự nối thừa sau khi cắt (vd: _, -, space)", Checked = true, AutoSize = true, Margin = new Padding(10, 6, 0, 0) };
        flowRemove.Controls.Add(chkTrimLeading);

        btnApplyRemove = new Button
        {
            Text = "Thực hiện xóa ký tự",
            Width = 145,
            Height = 28,
            BackColor = Color.FromArgb(235, 243, 252),
            Margin = new Padding(12, 3, 0, 0)
        };
        btnApplyRemove.Click += (s, e) => ExecuteRemoveStage();
        flowRemove.Controls.Add(btnApplyRemove);

        tabRemove.Controls.Add(flowRemove);
        tabFeatures.TabPages.Add(tabRemove);

        // TAB 3: Thêm tiền tố / hậu tố
        var tabAffix = new TabPage("3. Thêm tiền tố / hậu tố");
        var flowAffix = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(10)
        };

        flowAffix.Controls.Add(CreateLabel("Thêm vào đầu tên (Prefix):"));
        txtAddPrefix = new TextBox { Width = 140 };
        flowAffix.Controls.Add(txtAddPrefix);

        flowAffix.Controls.Add(CreateLabel("Thêm vào cuối tên (Suffix):"));
        txtAddSuffix = new TextBox { Width = 140 };
        flowAffix.Controls.Add(txtAddSuffix);

        btnApplyAffix = new Button
        {
            Text = "Thực hiện thêm chữ",
            Width = 145,
            Height = 28,
            BackColor = Color.FromArgb(235, 243, 252),
            Margin = new Padding(15, 3, 0, 0)
        };
        btnApplyAffix.Click += (s, e) => ExecuteAddAffixStage();
        flowAffix.Controls.Add(btnApplyAffix);

        tabAffix.Controls.Add(flowAffix);
        tabFeatures.TabPages.Add(tabAffix);

        // Sort Bar & Action controls
        var pnlSort = new Panel { Dock = DockStyle.Bottom, Height = 36, Padding = new Padding(2, 4, 0, 0) };
        var lblSort = CreateLabel("Sắp xếp danh sách:");
        lblSort.Location = new Point(4, 7);
        cboSort = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 185, Location = new Point(135, 4) };
        cboSort.Items.AddRange(new object[] {
            "Thứ tự chọn từ Explorer",
            "Tên A → Z (tự nhiên)",
            "Tên Z → A (tự nhiên)",
            "Ngày tạo cũ → mới",
            "Ngày tạo mới → cũ",
            "Sửa đổi cũ → mới",
            "Sửa đổi mới → cũ"
        });
        cboSort.SelectedIndex = 0; // Mặc định giữ nguyên thứ tự chọn từ Explorer
        cboSort.SelectedIndexChanged += (s, e) => SortItems();

        btnMoveUp = new Button { Text = "▲ Lên", Width = 65, Height = 26, Location = new Point(330, 4) };
        btnMoveUp.Click += (s, e) => MoveSelectedItem(-1);

        btnMoveDown = new Button { Text = "▼ Xuống", Width = 65, Height = 26, Location = new Point(400, 4) };
        btnMoveDown.Click += (s, e) => MoveSelectedItem(1);

        btnReset = new Button { Text = "Khôi phục tên gốc ban đầu", Width = 175, Height = 26, Location = new Point(475, 4) };
        btnReset.Click += (s, e) => ResetToOriginalNames();

        pnlSort.Controls.Add(lblSort);
        pnlSort.Controls.Add(cboSort);
        pnlSort.Controls.Add(btnMoveUp);
        pnlSort.Controls.Add(btnMoveDown);
        pnlSort.Controls.Add(btnReset);

        pnlTop.Controls.Add(tabFeatures);
        pnlTop.Controls.Add(pnlSort);
        table.Controls.Add(pnlTop, 0, 0);

        // Preview table
        var grpPreview = new GroupBox
        {
            Text = "Danh sách file (Các thay đổi được lưu tạm trên bảng, chỉ đổi tên thật khi ấn 'Áp dụng đổi tên')",
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };

        lvPreview = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = false
        };
        lvPreview.Columns.Add("STT", 50);
        lvPreview.Columns.Add("Tên gốc ban đầu", 380);
        lvPreview.Columns.Add("Tên mới sau khi xử lý (Tạm tính)", 430);

        grpPreview.Controls.Add(lvPreview);
        table.Controls.Add(grpPreview, 0, 1);

        // Bottom action bar
        var pnlBottom = new Panel { Dock = DockStyle.Fill, Height = 45 };

        lblCount = new Label
        {
            AutoSize = true,
            Location = new Point(0, 12),
            Text = $"Đã nạp: {_items.Count} file"
        };
        pnlBottom.Controls.Add(lblCount);

        btnCopyNames = new Button
        {
            Text = "📋 Sao chép danh sách tên",
            Width = 180,
            Height = 32,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlBottom.Width - 440, 6)
        };
        btnCopyNames.Click += (s, e) => CopyNamesToClipboard();

        btnApplyFinal = new Button
        {
            Text = "Áp dụng đổi tên",
            DialogResult = DialogResult.OK,
            Width = 140,
            Height = 32,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlBottom.Width - 250, 6),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnApplyFinal.Click += (s, e) => Close();

        btnCancel = new Button
        {
            Text = "Đóng",
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
            btnApplyFinal.Location = new Point(pnlBottom.Width - 250, 6);
            btnCopyNames.Location = new Point(pnlBottom.Width - 440, 6);
        };

        pnlBottom.Controls.Add(btnCopyNames);
        pnlBottom.Controls.Add(btnApplyFinal);
        pnlBottom.Controls.Add(btnCancel);
        table.Controls.Add(pnlBottom, 0, 2);

        Controls.Add(table);
        AcceptButton = btnApplyFinal;
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

    // Giai đoạn 1: Đánh số
    private void ExecuteNumberingStage()
    {
        int start = (int)numStart.Value;
        int step = (int)numStep.Value;
        int padding = (int)numPadding.Value;
        string prefix = txtPrefix.Text;
        string suffix = txtSuffix.Text;
        string sep = txtSeparator.Text;
        bool keepName = chkKeepName.Checked;
        bool isNumberPrefix = cboPosition.SelectedIndex == 0;

        for (int i = 0; i < _items.Count; i++)
        {
            long numVal = start + (long)i * step;
            string numStr = numVal.ToString().PadLeft(padding, '0');
            string currentName = _items[i].WorkingName;

            string core;
            if (!keepName)
            {
                core = numStr;
            }
            else
            {
                if (isNumberPrefix)
                {
                    core = string.IsNullOrEmpty(sep) ? $"{numStr}{currentName}" : $"{numStr}{sep}{currentName}";
                }
                else
                {
                    core = string.IsNullOrEmpty(sep) ? $"{currentName}{numStr}" : $"{currentName}{sep}{numStr}";
                }
            }

            _items[i].WorkingName = $"{prefix}{core}{suffix}";
        }

        UpdateListView();
    }

    // Giai đoạn 2: Xóa trong khoảng (1-indexed)
    private void ExecuteRemoveStage()
    {
        int from = (int)numRemoveFrom.Value;
        int to = (int)numRemoveTo.Value;
        if (from > to)
        {
            int t = from;
            from = to;
            to = t;
        }

        bool trim = chkTrimLeading.Checked;

        for (int i = 0; i < _items.Count; i++)
        {
            string name = _items[i].WorkingName;
            int len = name.Length;

            if (from <= len)
            {
                int startIndex = from - 1;
                int count = Math.Min(to - from + 1, len - startIndex);
                string newName = name.Remove(startIndex, count);

                if (trim)
                {
                    newName = newName.TrimStart(' ', '-', '_', '.');
                }

                if (!string.IsNullOrEmpty(newName))
                {
                    _items[i].WorkingName = newName;
                }
            }
        }

        UpdateListView();
    }

    // Giai đoạn 3: Thêm tiền tố / hậu tố
    private void ExecuteAddAffixStage()
    {
        string p = txtAddPrefix.Text;
        string s = txtAddSuffix.Text;

        if (string.IsNullOrEmpty(p) && string.IsNullOrEmpty(s))
            return;

        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].WorkingName = $"{p}{_items[i].WorkingName}{s}";
        }

        UpdateListView();
    }

    // Khôi phục tên gốc
    private void ResetToOriginalNames()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].WorkingName = Path.GetFileNameWithoutExtension(_items[i].OriginalPath);
        }
        UpdateListView();
    }

    // Di chuyển vị trí file lên / xuống
    private void MoveSelectedItem(int direction)
    {
        if (lvPreview.SelectedIndices.Count == 0) return;
        int selectedIndex = lvPreview.SelectedIndices[0];
        int targetIndex = selectedIndex + direction;

        if (targetIndex < 0 || targetIndex >= _items.Count) return;

        var temp = _items[selectedIndex];
        _items[selectedIndex] = _items[targetIndex];
        _items[targetIndex] = temp;

        UpdateListView();
        lvPreview.Items[targetIndex].Selected = true;
        lvPreview.Items[targetIndex].EnsureVisible();
    }

    // Sắp xếp danh sách
    private void SortItems()
    {
        switch (cboSort.SelectedIndex)
        {
            case 1: // NameAsc
                _items.Sort((a, b) => StrCmpLogicalW(a.OriginalFileName, b.OriginalFileName));
                break;
            case 2: // NameDesc
                _items.Sort((a, b) => StrCmpLogicalW(b.OriginalFileName, a.OriginalFileName));
                break;
            case 3: // DateCreatedAsc
                _items.Sort((a, b) => File.GetCreationTimeUtc(a.OriginalPath).CompareTo(File.GetCreationTimeUtc(b.OriginalPath)));
                break;
            case 4: // DateCreatedDesc
                _items.Sort((a, b) => File.GetCreationTimeUtc(b.OriginalPath).CompareTo(File.GetCreationTimeUtc(a.OriginalPath)));
                break;
            case 5: // DateModifiedAsc
                _items.Sort((a, b) => File.GetLastWriteTimeUtc(a.OriginalPath).CompareTo(File.GetLastWriteTimeUtc(b.OriginalPath)));
                break;
            case 6: // DateModifiedDesc
                _items.Sort((a, b) => File.GetLastWriteTimeUtc(b.OriginalPath).CompareTo(File.GetLastWriteTimeUtc(a.OriginalPath)));
                break;
            case 0: // Thứ tự ban đầu
            default:
                break;
        }
        UpdateListView();
    }

    // Sao chép tên vào Clipboard
    private void CopyNamesToClipboard()
    {
        var sb = new StringBuilder();
        foreach (var item in _items)
        {
            sb.AppendLine(item.FinalFileName);
        }

        try
        {
            Clipboard.SetText(sb.ToString());
            MessageBox.Show(
                $"Đã sao chép danh sách {_items.Count} tên mới vào Clipboard!",
                "Thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể sao chép: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateListView()
    {
        lvPreview.BeginUpdate();
        lvPreview.Items.Clear();

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var lvItem = new ListViewItem((i + 1).ToString());
            lvItem.SubItems.Add(item.OriginalFileName);
            lvItem.SubItems.Add(item.FinalFileName);

            if (item.OriginalFileName != item.FinalFileName)
            {
                lvItem.ForeColor = Color.DarkBlue;
            }

            lvPreview.Items.Add(lvItem);
        }

        lvPreview.EndUpdate();
    }
}
