# MultipleFileHelper - Tiện ích xử lý nhiều file (Windows Context Menu)

Công cụ tích hợp trực tiếp vào menu chuột phải Windows Explorer giúp bạn xử lý đổi tên hàng loạt file một cách an toàn, mượt mà và trực quan.

## 🚀 Tính năng nổi bật
1. **Đánh số thứ tự hàng loạt:**
   - Tùy chỉnh số bắt đầu (`Start Number`), bước nhảy (`Step`).
   - Padding số `0` đệm phía trước (`001`, `002`, `0001`,...).
   - Thêm tiền tố (`Prefix`), hậu tố (`Suffix`), ký tự nối tự do (`_`, `-`, space, rỗng,...).
   - Chọn vị trí đánh số: ở **Đầu tên** hoặc **Cuối tên**.
   - Tùy chọn giữ lại tên gốc hoặc thay thế hoàn toàn bằng số thứ tự.
2. **Xóa N ký tự đầu:**
   - Cắt bỏ nhanh số lượng $N$ ký tự ở đầu tên file.
   - Tùy chọn tự động dọn dẹp ký tự nối thừa (dấu cách, `_`, `-`, `.`) ở đầu tên mới.
3. **Sắp xếp linh hoạt trước khi đổi tên:**
   - Sắp xếp tên theo thứ tự tự nhiên (A → Z, Z → A như Windows Explorer).
   - Sắp xếp theo ngày tạo hoặc ngày sửa đổi gần nhất/cũ nhất.
4. **Xem trước trực quan (Live Preview):**
   - Xem ngay danh sách bảng tên gốc → tên mới trước khi áp dụng.
5. **Cơ chế đổi tên 2 giai đoạn (Safe Two-Phase Rename):**
   - Đổi tên qua file trung gian `.tmp` để chống xung đột vòng tròn và tự động rollback nếu gặp lỗi.
6. **Không đơ lag máy:**
   - Sử dụng IPC Named Pipe + Mutex gom nhóm file trong 1 tiến trình duy nhất.

---

## 📦 Cách cài đặt cho máy bất kỳ
1. Tải thư mục chứa `publish\MultipleFileHelper.exe` (hoặc đặt file `MultipleFileHelper.exe` cạnh installer).
2. Chuột phải vào **`Install.bat`** (hoặc `Install.ps1`) chọn **Run** (không yêu cầu quyền Administrator).
3. Ứng dụng sẽ tự động sao chép vào `%LocalAppData%\Programs\MultipleFileHelper` và gắn menu vào Explorer.

---

## 🗑️ Cách gỡ cài đặt
- Chạy file **`Uninstall.bat`** (hoặc `Uninstall.ps1`). 
- Toàn bộ registry menu chuột phải và thư mục ứng dụng sẽ được dọn dẹp sạch sẽ 100%.
