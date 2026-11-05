use crate::PlatformError;
use std::ffi::OsStr;
use std::os::windows::ffi::OsStrExt;
use std::path::Path;
use windows_sys::Win32::Foundation::*;
use windows_sys::Win32::System::Registry::*;

pub struct WindowsContextMenu;

fn to_wide(s: &str) -> Vec<u16> {
    OsStr::new(s).encode_wide().chain(Some(0)).collect()
}

fn set_registry_value(hkey: HKEY, name: &str, value: &str) -> Result<(), PlatformError> {
    let wide_name = to_wide(name);
    let wide_val = to_wide(value);
    unsafe {
        let status = RegSetValueExW(
            hkey,
            if name.is_empty() { std::ptr::null() } else { wide_name.as_ptr() },
            0,
            REG_SZ,
            wide_val.as_ptr() as *const u8,
            (wide_val.len() * 2) as u32,
        );
        if status != ERROR_SUCCESS {
            return Err(PlatformError::Registry(format!("RegSetValueExW failed: {}", status)));
        }
    }
    Ok(())
}

fn create_key(parent: HKEY, subkey: &str) -> Result<HKEY, PlatformError> {
    let wide_sub = to_wide(subkey);
    let mut hkey: HKEY = std::ptr::null_mut();
    unsafe {
        let status = RegCreateKeyExW(
            parent,
            wide_sub.as_ptr(),
            0,
            std::ptr::null_mut(),
            0,
            KEY_WRITE | KEY_READ,
            std::ptr::null_mut(),
            &mut hkey,
            std::ptr::null_mut(),
        );
        if status != ERROR_SUCCESS {
            return Err(PlatformError::Registry(format!("RegCreateKeyExW failed: {}", status)));
        }
    }
    Ok(hkey)
}

fn close_key(hkey: HKEY) {
    unsafe {
        if !hkey.is_null() {
            RegCloseKey(hkey);
        }
    }
}

impl WindowsContextMenu {
    /// Đăng ký Context menu vào HKCU bằng Win32 API gốc (không qua PowerShell, siêu nhanh < 2ms)
    pub fn install(exe_path: &Path) -> Result<(), PlatformError> {
        let exe_str = exe_path.to_string_lossy();

        // 1. Root Key: Software\Classes\*\shell\FilePilot
        let root = create_key(HKEY_CURRENT_USER, r"Software\Classes\*\shell\FilePilot")?;
        set_registry_value(root, "MUIVerb", "FilePilot - Xử lý hàng loạt")?;
        set_registry_value(root, "SubCommands", "")?;
        set_registry_value(root, "Icon", "shell32.dll,133")?;
        set_registry_value(root, "MultiSelectModel", "Player")?;

        // 2. Submenu 1: Numbering
        let sub1 = create_key(root, r"shell\01_Numbering")?;
        set_registry_value(sub1, "MUIVerb", "Đánh số thứ tự")?;
        set_registry_value(sub1, "MultiSelectModel", "Player")?;
        set_registry_value(sub1, "Icon", "shell32.dll,264")?;
        let sub1_cmd = create_key(sub1, "command")?;
        set_registry_value(sub1_cmd, "", &format!("\"{}\" --action=numbering \"%1\"", exe_str))?;
        close_key(sub1_cmd);
        close_key(sub1);

        // 3. Submenu 2: Remove Prefix
        let sub2 = create_key(root, r"shell\02_RemovePrefix")?;
        set_registry_value(sub2, "MUIVerb", "Xóa ký tự đầu")?;
        set_registry_value(sub2, "MultiSelectModel", "Player")?;
        set_registry_value(sub2, "Icon", "shell32.dll,131")?;
        let sub2_cmd = create_key(sub2, "command")?;
        set_registry_value(sub2_cmd, "", &format!("\"{}\" --action=remove-prefix \"%1\"", exe_str))?;
        close_key(sub2_cmd);
        close_key(sub2);

        // 4. Submenu 3: Copy Names
        let sub3 = create_key(root, r"shell\03_CopyNames")?;
        set_registry_value(sub3, "MUIVerb", "Sao chép tên")?;
        set_registry_value(sub3, "MultiSelectModel", "Player")?;
        set_registry_value(sub3, "Icon", "shell32.dll,134")?;
        let sub3_cmd = create_key(sub3, "command")?;
        set_registry_value(sub3_cmd, "", &format!("\"{}\" --action=copy-names \"%1\"", exe_str))?;
        close_key(sub3_cmd);
        close_key(sub3);

        close_key(root);
        Ok(())
    }

    /// Gỡ bỏ Context menu
    pub fn uninstall() -> Result<(), PlatformError> {
        let wide_sub = to_wide(r"Software\Classes\*\shell\FilePilot");
        unsafe {
            RegDeleteTreeW(HKEY_CURRENT_USER, wide_sub.as_ptr());
        }
        Ok(())
    }
}
