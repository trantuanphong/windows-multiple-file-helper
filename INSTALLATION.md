# Hướng Dẫn Cài Đặt & Sử Dụng FilePilot v2

FilePilot là công cụ xử lý và đổi tên file hàng loạt siêu nhẹ (<1MB), hiệu năng cao, tích hợp trực tiếp vào chuột phải (Context Menu) của hệ điều hành, không phụ thuộc runtime bên ngoài (.NET, Java, Electron).

---

## 1. Yêu cầu hệ thống

- **Windows:** Windows 10 / 11 (x64 hoặc ARM64). Không yêu cầu quyền Administrator.
- **macOS:** macOS 11 Big Sur trở lên (Intel hoặc Apple Silicon).
- **Linux:** Bản phân phối phổ biến (Ubuntu, Fedora, Arch, Debian) dùng KDE Plasma (Dolphin) hoặc GNOME (Nautilus).

---

## 2. Hướng dẫn cài đặt theo hệ điều hành

### Windows

#### Cách 1: Cài đặt tự động qua CLI (Khuyên dùng)
1. Tải hoặc build file `filepilot-cli.exe` (nằm tại thư mục `target/release/filepilot-cli.exe`).
2. Mở PowerShell/CMD tại thư mục chứa file và chạy lệnh:
   ```powershell
   .\filepilot-cli.exe install
   ```
3. Lệnh trên sẽ tự động:
   - Đăng ký menu ngữ cảnh vào Registry của người dùng hiện tại (`HKCU\Software\Classes\*\shell\FilePilot`).
   - Kích hoạt chế độ `MultiSelectModel=Player` gom hàng loạt file mà không mở thừa cửa sổ.
   - Không yêu cầu quyền Administrator.

#### Cách 2: Gỡ cài đặt
Khi không muốn dùng nữa, chỉ cần chạy:
```powershell
.\filepilot-cli.exe uninstall
```
Toàn bộ menu chuột phải sẽ được xóa sạch khỏi Registry.

---

### macOS

1. Sao chép binary `filepilot-cli` vào thư mục nhị phân cá nhân:
   ```bash
   chmod +x native/macos/install.sh
   ./native/macos/install.sh
   ```
   Binary sẽ được đặt tại `~/.local/bin/filepilot`.
2. Tạo **Quick Action** trong Finder:
   - Mở ứng dụng **Automator** trên Mac -> Chọn **New Document** -> **Quick Action**.
   - Mục **Workflow receives current**: chọn `files or folders` trong `Finder`.
   - Tìm action **Run Shell Script** ở thanh bên trái, kéo vào quy trình và dán lệnh:
     ```bash
     ~/.local/bin/filepilot --action=numbering "$@"
     ```
   - Nhấn `Cmd + S` và đặt tên (ví dụ: `FilePilot - Đánh số thứ tự`).
3. Chuột phải vào các file trong Finder -> **Quick Actions** -> chọn lệnh vừa tạo.

---

### Linux

#### Dành cho KDE Dolphin:
Sao chép tệp `.desktop` vào thư mục Service Menus của người dùng:
```bash
# Đảm bảo filepilot-cli nằm trong PATH (ví dụ /usr/local/bin hoặc ~/.local/bin)
sudo cp target/release/filepilot-cli /usr/local/bin/

# Cài đặt menu chuột phải cho Dolphin
mkdir -p ~/.local/share/kservices5/ServiceMenus/
cp native/linux/filepilot.desktop ~/.local/share/kservices5/ServiceMenus/
```

#### Dành cho GNOME Nautilus:
Tạo script trực tiếp trong thư mục Nautilus Scripts:
```bash
mkdir -p ~/.local/share/nautilus/scripts/
cat << 'EOF' > ~/.local/share/nautilus/scripts/FilePilot-Numbering
#!/bin/bash
filepilot-cli --action=numbering "$@"
EOF
chmod +x ~/.local/share/nautilus/scripts/FilePilot-Numbering
```

---

## 3. Hướng dẫn sử dụng

### 3.1. Sử dụng qua chuột phải (Context Menu)

1. Chọn một hoặc nhiều file trong trình quản lý file (File Explorer / Finder / Dolphin).
2. Chuột phải vào vùng chọn:
   - **Đánh số thứ tự (`numbering`):** Tự động đánh số theo thứ tự A-Z dạng `001_tên-file.ext`, `002_tên-file.ext`.
   - **Xóa ký tự đầu (`remove-prefix`):** Cắt bỏ các tiền tố số thứ tự cũ (ví dụ: `01 - `, `02_`) và tự dọn dẹp khoảng trắng, dấu gạch nối thừa ở đầu tên mới.
   - **Sao chép tên (`copy-names`):** Gom toàn bộ tên của các file được chọn và chép vào Clipboard để dán nhanh vào Excel, Word, ghi chú.

---

### 3.2. Sử dụng qua dòng lệnh (CLI)

FilePilot hỗ trợ gọi trực tiếp qua CLI cho các tác vụ tự động hóa hoặc script hàng loạt:

#### Đánh số thứ tự danh sách file:
```bash
filepilot-cli --action=numbering file1.jpg file2.jpg file3.jpg
```

#### Xóa tiền tố ký tự:
```bash
filepilot-cli --action=remove-prefix "01 - bao_cao.docx" "02 - de_xuat.docx"
```

#### Xuất danh sách tên ra Clipboard:
```bash
filepilot-cli --action=copy-names E:\Photos\*.png
```

---

## 4. Cơ chế an toàn dữ liệu

FilePilot v2 được thiết kế với tiêu chí an toàn dữ liệu hàng đầu:
1. **Chống va chạm vòng lặp (Cyclic Conflict):** Khi bạn đổi `A -> B` và `B -> A`, ứng dụng tự động đi qua file tạm trung gian (`.fp_tmp_...`) để tránh ghi đè làm mất file.
2. **Operation Journal:** Trạng thái từng file được ghi ra đĩa tại thư mục tạm (`%TEMP%/filepilot_journal/active.json`) trước khi đổi tên. Nếu máy mất điện hoặc tiến trình bị ngắt giữa chừng, lần chạy tiếp theo sẽ tự động khôi phục (Rollback) các file dở dang về trạng thái ban đầu.
3. **Kiểm tra trước khi thực hiện (Preflight Check):** Nếu tên đích đã tồn tại trên ổ đĩa và không thuộc nhóm file đang hoán đổi, thao tác sẽ bị từ chối ngay lập tức để bảo vệ file có sẵn.
