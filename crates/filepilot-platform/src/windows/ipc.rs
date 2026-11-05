use crate::PlatformError;
use std::ffi::OsStr;
use std::os::windows::ffi::OsStrExt;
use std::path::PathBuf;
use std::ptr::null_mut;
use std::time::{Duration, Instant};
use windows_sys::Win32::Foundation::*;
use windows_sys::Win32::Storage::FileSystem::*;
use windows_sys::Win32::System::IO::*;
use windows_sys::Win32::System::Pipes::*;
use windows_sys::Win32::System::Threading::*;

pub const PIPE_NAME: &str = r"\\.\pipe\FilePilot_Unified_Pipe";
pub const MUTEX_NAME: &str = r"Global\FilePilot_Unified_Mutex";

pub struct SingleInstanceMutex {
    handle: HANDLE,
    is_primary: bool,
}

impl SingleInstanceMutex {
    pub fn acquire(name: &str) -> Result<Self, PlatformError> {
        let wide_name: Vec<u16> = OsStr::new(name).encode_wide().chain(Some(0)).collect();
        unsafe {
            let handle = CreateMutexW(null_mut(), 1, wide_name.as_ptr());
            if handle.is_null() {
                return Err(PlatformError::Ipc("Failed to create mutex".to_string()));
            }
            let is_primary = GetLastError() != ERROR_ALREADY_EXISTS;
            Ok(Self { handle, is_primary })
        }
    }

    pub fn is_primary(&self) -> bool {
        self.is_primary
    }
}

impl Drop for SingleInstanceMutex {
    fn drop(&mut self) {
        unsafe {
            if !self.handle.is_null() {
                ReleaseMutex(self.handle);
                CloseHandle(self.handle);
            }
        }
    }
}

pub struct IpcClient;

impl IpcClient {
    pub fn send_paths(pipe_name: &str, paths: &[PathBuf]) -> Result<(), PlatformError> {
        if paths.is_empty() {
            return Ok(());
        }

        let wide_pipe: Vec<u16> = OsStr::new(pipe_name).encode_wide().chain(Some(0)).collect();
        unsafe {
            let handle = CreateFileW(
                wide_pipe.as_ptr(),
                GENERIC_WRITE,
                0,
                null_mut(),
                OPEN_EXISTING,
                0,
                null_mut(),
            );

            if handle == INVALID_HANDLE_VALUE {
                return Err(PlatformError::Ipc("Cannot connect to primary pipe".to_string()));
            }

            let mut payload = String::new();
            for p in paths {
                payload.push_str(&p.to_string_lossy());
                payload.push('\n');
            }

            let bytes = payload.as_bytes();
            let mut written = 0;
            let success = WriteFile(
                handle,
                bytes.as_ptr(),
                bytes.len() as u32,
                &mut written,
                null_mut(),
            );

            CloseHandle(handle);

            if success == 0 {
                return Err(PlatformError::Ipc("Failed to write to pipe".to_string()));
            }
        }

        Ok(())
    }
}

pub struct IpcServer;

impl IpcServer {
    /// Thu thập paths gửi đến từ các instance thứ cấp trong vòng timeout_ms
    pub fn collect_paths(pipe_name: &str, timeout: Duration) -> Vec<PathBuf> {
        let mut results = Vec::new();
        let wide_pipe: Vec<u16> = OsStr::new(pipe_name).encode_wide().chain(Some(0)).collect();

        unsafe {
            let handle = CreateNamedPipeW(
                wide_pipe.as_ptr(),
                PIPE_ACCESS_INBOUND | FILE_FLAG_FIRST_PIPE_INSTANCE,
                PIPE_TYPE_BYTE | PIPE_READMODE_BYTE | PIPE_NOWAIT,
                PIPE_UNLIMITED_INSTANCES,
                65536,
                65536,
                0,
                null_mut(),
            );

            if handle == INVALID_HANDLE_VALUE {
                return results;
            }

            let start = Instant::now();
            while start.elapsed() < timeout {
                let connected = ConnectNamedPipe(handle, null_mut()) != 0
                    || GetLastError() == ERROR_PIPE_CONNECTED;

                if connected {
                    let mut buffer = [0u8; 16384];
                    let mut bytes_read = 0;
                    if ReadFile(
                        handle,
                        buffer.as_mut_ptr(),
                        buffer.len() as u32,
                        &mut bytes_read,
                        null_mut(),
                    ) != 0 && bytes_read > 0 {
                        if let Ok(text) = std::str::from_utf8(&buffer[..bytes_read as usize]) {
                            for line in text.lines() {
                                let trimmed = line.trim();
                                if !trimmed.is_empty() {
                                    results.push(PathBuf::from(trimmed));
                                }
                            }
                        }
                    }
                    DisconnectNamedPipe(handle);
                } else {
                    Sleep(10);
                }
            }

            CloseHandle(handle);
        }

        results
    }
}
