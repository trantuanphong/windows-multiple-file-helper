#[cfg(target_os = "windows")]
pub mod windows;

#[cfg(target_os = "windows")]
pub use windows::ipc::{IpcClient, IpcServer, SingleInstanceMutex};

#[derive(Debug, thiserror::Error)]
pub enum PlatformError {
    #[error("IPC Error: {0}")]
    Ipc(String),

    #[error("Registry Error: {0}")]
    Registry(String),

    #[error("Platform unsupported")]
    Unsupported,
}
