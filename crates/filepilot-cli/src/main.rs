use clap::{Parser, Subcommand};
use filepilot_core::{
    NumberingConfig, NumberingPosition, OperationJournal, RenamePlan, Renamer, SortMode, TrimConfig,
};
#[cfg(target_os = "windows")]
use filepilot_platform::{
    windows::ipc::{MUTEX_NAME, PIPE_NAME},
    windows::registry::WindowsContextMenu,
    IpcClient, IpcServer, SingleInstanceMutex,
};
use std::collections::HashSet;
use std::path::PathBuf;
#[cfg(target_os = "windows")]
use std::time::Duration;

#[derive(Parser, Debug)]
#[command(author, version, about = "FilePilot - Công cụ xử lý file hàng loạt siêu nhẹ & siêu nhanh", long_about = None)]
struct Cli {
    #[arg(short, long)]
    action: Option<String>,

    /// Cài đặt hoặc gỡ cài đặt menu chuột phải
    #[command(subcommand)]
    command: Option<Commands>,

    /// Danh sách file truyền vào
    files: Vec<PathBuf>,
}

#[derive(Subcommand, Debug)]
enum Commands {
    /// Đăng ký vào Context Menu của OS
    Install,
    /// Gỡ khỏi Context Menu của OS
    Uninstall,
}

fn main() {
    let args = Cli::parse();

    #[cfg(target_os = "windows")]
    if let Some(cmd) = args.command {
        let exe = std::env::current_exe().unwrap_or_else(|_| PathBuf::from("filepilot-cli.exe"));
        match cmd {
            Commands::Install => {
                if let Err(e) = WindowsContextMenu::install(&exe) {
                    eprintln!("Lỗi cài đặt context menu: {}", e);
                } else {
                    println!("Đã cài đặt thành công Context Menu FilePilot!");
                }
            }
            Commands::Uninstall => {
                if let Err(e) = WindowsContextMenu::uninstall() {
                    eprintln!("Lỗi gỡ bỏ context menu: {}", e);
                } else {
                    println!("Đã gỡ bỏ thành công Context Menu FilePilot!");
                }
            }
        }
        return;
    }

    let direct_files: Vec<PathBuf> = args
        .files
        .into_iter()
        .filter(|p| p.exists())
        .collect();

    #[cfg(target_os = "windows")]
    let direct_files = {
        let mut direct_files = direct_files;
        // 1. Kiểm tra Single Instance Mutex để gom file khi Explorer gọi nhiều process song song
        let mutex = match SingleInstanceMutex::acquire(MUTEX_NAME) {
            Ok(m) => m,
            Err(e) => {
                eprintln!("Lỗi khởi tạo Mutex: {}", e);
                return;
            }
        };

        if !mutex.is_primary() {
            // Là process con: gửi file sang process chính rồi thoát ngay lập tức (<5ms)
            let _ = IpcClient::send_paths(PIPE_NAME, &direct_files);
            return;
        }

        // Là process chính: chờ thu thập file từ các sub-instances nếu có
        let extra = IpcServer::collect_paths(PIPE_NAME, Duration::from_millis(100));
        direct_files.extend(extra);
        direct_files
    };

    // Lọc trùng lặp và kiểm tra tồn tại
    let mut seen = HashSet::new();
    let mut files: Vec<PathBuf> = Vec::new();
    for f in direct_files {
        let norm = f.to_string_lossy().to_lowercase();
        if seen.insert(norm) && f.exists() {
            files.push(f);
        }
    }

    if files.is_empty() {
        println!("Không có file hợp lệ nào được chọn.");
        return;
    }

    // Sắp xếp tự nhiên mặc định (A-Z)
    filepilot_core::sort::sort_paths(&mut files, SortMode::NameAsc);

    let action = args.action.as_deref().unwrap_or("numbering");

    match action {
        "copy-names" => {
            let mut text = String::new();
            for f in &files {
                if let Some(name) = f.file_name().and_then(|s| s.to_str()) {
                    text.push_str(name);
                    text.push('\n');
                }
            }

            if let Ok(mut clipboard) = arboard::Clipboard::new() {
                if clipboard.set_text(text).is_ok() {
                    println!("Đã sao chép {} tên file vào Clipboard!", files.len());
                    return;
                }
            }
            eprintln!("Lỗi: Không thể truy cập Clipboard.");
        }
        "remove-prefix" => {
            let config = TrimConfig {
                count: 3,
                trim_separators: true,
            };
            let items = Renamer::plan_trim_prefix(&files, &config);
            execute_items(&items);
        }
        "numbering" | _ => {
            let config = NumberingConfig {
                start: 1,
                step: 1,
                digits: 3,
                prefix: String::new(),
                suffix: String::new(),
                separator: "_".to_string(),
                position: NumberingPosition::Prefix,
                keep_original_name: true,
            };
            let items = Renamer::plan_numbering(&files, &config);
            execute_items(&items);
        }
    }
}

fn execute_items(items: &[filepilot_core::RenameItem]) {
    let plans: Vec<RenamePlan> = items
        .iter()
        .filter(|it| it.is_changed())
        .map(|it| RenamePlan {
            from: it.original_path.clone(),
            to: it.new_path.clone(),
        })
        .collect();

    if plans.is_empty() {
        println!("Không có file nào cần đổi tên.");
        return;
    }

    let journal_dir = std::env::temp_dir().join("filepilot_journal");
    let journal = OperationJournal::new(journal_dir.join("active.json"));

    match Renamer::execute_plans(&plans, &journal) {
        Ok(count) => {
            println!("Đã xử lý an toàn {}/{} file thành công!", count, plans.len());
        }
        Err(e) => {
            eprintln!("Thao tác thất bại (đã an toàn rollback): {}", e);
        }
    }
}
