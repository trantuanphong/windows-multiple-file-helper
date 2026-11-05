use crate::error::CoreError;
use crate::journal::{JournalEntry, JournalStep, OperationJournal, StepState};
use crate::model::{NumberingConfig, NumberingPosition, RenameItem, RenamePlan, ReplaceConfig, TrimConfig};
use std::collections::HashSet;
use std::fs;
use std::path::{Path, PathBuf};
use std::time::{SystemTime, UNIX_EPOCH};
use std::sync::atomic::{AtomicU64, Ordering};

static COUNTER: AtomicU64 = AtomicU64::new(1);

fn generate_unique_token() -> String {
    let now = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .map(|d| d.as_nanos())
        .unwrap_or(0);
    let seq = COUNTER.fetch_add(1, Ordering::Relaxed);
    format!("{:x}_{:x}", now, seq)
}

pub struct Renamer;

impl Renamer {
    /// Sinh danh sách xem trước cho đánh số thứ tự
    pub fn plan_numbering(paths: &[PathBuf], config: &NumberingConfig) -> Vec<RenameItem> {
        let mut items = Vec::with_capacity(paths.len());
        let mut cur = config.start;

        for path in paths {
            let file_stem = path.file_stem().and_then(|s| s.to_str()).unwrap_or("");
            let extension = path.extension().and_then(|s| s.to_str());

            let num_str = format!("{:0width$}", cur, width = config.digits);
            let num_block = format!("{}{}{}", config.prefix, num_str, config.suffix);

            let new_stem = match config.position {
                NumberingPosition::Prefix => {
                    if config.keep_original_name && !file_stem.is_empty() {
                        format!("{}{}{}", num_block, config.separator, file_stem)
                    } else {
                        num_block
                    }
                }
                NumberingPosition::Suffix => {
                    if config.keep_original_name && !file_stem.is_empty() {
                        format!("{}{}{}", file_stem, config.separator, num_block)
                    } else {
                        num_block
                    }
                }
            };

            let new_name = match extension {
                Some(ext) if !ext.is_empty() => format!("{}.{}", new_stem, ext),
                _ => new_stem,
            };

            items.push(RenameItem::new(path, new_name));
            cur += config.step;
        }

        items
    }

    /// Sinh danh sách xem trước cho xóa ký tự ở đầu
    pub fn plan_trim_prefix(paths: &[PathBuf], config: &TrimConfig) -> Vec<RenameItem> {
        let mut items = Vec::with_capacity(paths.len());

        for path in paths {
            let file_stem = path.file_stem().and_then(|s| s.to_str()).unwrap_or("");
            let extension = path.extension().and_then(|s| s.to_str());

            let mut chars: Vec<char> = file_stem.chars().collect();
            let count = config.count.min(chars.len());
            let remaining: String = chars.drain(count..).collect();

            let mut new_stem = remaining;
            if config.trim_separators {
                new_stem = new_stem
                    .trim_start_matches(|c: char| c.is_whitespace() || c == '_' || c == '-' || c == '.')
                    .to_string();
            }

            if new_stem.is_empty() {
                // Giữ lại tên gốc nếu cắt hết để an toàn
                new_stem = file_stem.to_string();
            }

            let new_name = match extension {
                Some(ext) if !ext.is_empty() => format!("{}.{}", new_stem, ext),
                _ => new_stem,
            };

            items.push(RenameItem::new(path, new_name));
        }

        items
    }

    /// Sinh danh sách xem trước cho thay thế chuỗi (Search & Replace)
    pub fn plan_replace(paths: &[PathBuf], config: &ReplaceConfig) -> Vec<RenameItem> {
        let mut items = Vec::with_capacity(paths.len());

        for path in paths {
            let file_stem = path.file_stem().and_then(|s| s.to_str()).unwrap_or("");
            let extension = path.extension().and_then(|s| s.to_str());

            let new_stem = if config.search.is_empty() {
                file_stem.to_string()
            } else if config.match_case {
                file_stem.replace(&config.search, &config.replace)
            } else {
                let search_lower = config.search.to_lowercase();
                let stem_lower = file_stem.to_lowercase();
                let mut result = String::new();
                let mut last = 0;
                for (idx, _) in stem_lower.match_indices(&search_lower) {
                    result.push_str(&file_stem[last..idx]);
                    result.push_str(&config.replace);
                    last = idx + config.search.len();
                }
                result.push_str(&file_stem[last..]);
                result
            };

            let new_name = match extension {
                Some(ext) if !ext.is_empty() => format!("{}.{}", new_stem, ext),
                _ => new_stem,
            };

            items.push(RenameItem::new(path, new_name));
        }

        items
    }

    /// Thực hiện đổi tên 2 giai đoạn kèm Operation Journal
    pub fn execute_plans(plans: &[RenamePlan], journal: &OperationJournal) -> Result<usize, CoreError> {
        if plans.is_empty() {
            return Ok(0);
        }

        // 1. Kiểm tra va chạm trong danh sách đích (Case-insensitive)
        let mut targets = HashSet::new();
        for plan in plans {
            let target_str = plan.to.to_string_lossy().to_lowercase();
            if !targets.insert(target_str) {
                return Err(CoreError::DuplicateTarget(plan.to.to_string_lossy().to_string()));
            }

            // 2. Kiểm tra nếu file đích đã tồn tại ngoài đĩa và không phải chính file nguồn
            if plan.to.exists() && plan.from != plan.to {
                // Kiểm tra khác file thật sự (tránh false-positive khi chỉ đổi hoa/thường trên Windows)
                let from_canon = fs::canonicalize(&plan.from).unwrap_or_else(|_| plan.from.clone());
                let to_canon = fs::canonicalize(&plan.to).unwrap_or_else(|_| plan.to.clone());
                if from_canon != to_canon {
                    return Err(CoreError::DestinationExists(plan.to.to_string_lossy().to_string()));
                }
            }
        }

        // 3. Khởi tạo Journal
        let mut steps = Vec::with_capacity(plans.len());
        for plan in plans {
            let dir = plan.from.parent().unwrap_or_else(|| Path::new(""));
            let temp_name = format!(".fp_tmp_{}.tmp", generate_unique_token());
            let temp_path = dir.join(temp_name);
            steps.push(JournalStep {
                original: plan.from.clone(),
                temp: temp_path,
                target: plan.to.clone(),
                state: StepState::Pending,
            });
        }

        let mut entry = JournalEntry {
            id: generate_unique_token(),
            timestamp: chrono::Utc::now().timestamp(),
            steps,
        };

        journal.save(&entry)?;

        // Phase 1: Chuyển toàn bộ sang .tmp
        for step in entry.steps.iter_mut() {
            if let Err(e) = fs::rename(&step.original, &step.temp) {
                // Rollback ngay các file đã chuyển
                rollback_internal(&entry.steps);
                journal.clear().ok();
                return Err(CoreError::Io(e));
            }
            step.state = StepState::MovedToTemp;
        }
        journal.save(&entry)?;

        // Phase 2: Chuyển từ .tmp sang đích
        for step in entry.steps.iter_mut() {
            if let Err(e) = fs::rename(&step.temp, &step.target) {
                // Rollback
                rollback_internal(&entry.steps);
                journal.clear().ok();
                return Err(CoreError::Io(e));
            }
            step.state = StepState::Completed;
        }

        journal.clear()?;
        Ok(plans.len())
    }
}

fn rollback_internal(steps: &[JournalStep]) {
    for step in steps.iter().rev() {
        match step.state {
            StepState::Completed => {
                if step.target.exists() && !step.original.exists() {
                    let _ = fs::rename(&step.target, &step.original);
                }
            }
            StepState::MovedToTemp => {
                if step.temp.exists() && !step.original.exists() {
                    let _ = fs::rename(&step.temp, &step.original);
                }
            }
            _ => {}
        }
    }
}
