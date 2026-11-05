use crate::error::CoreError;
use serde::{Deserialize, Serialize};
use std::fs::{self, OpenOptions};
use std::io::{Read, Write};
use std::path::PathBuf;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct JournalStep {
    pub original: PathBuf,
    pub temp: PathBuf,
    pub target: PathBuf,
    pub state: StepState,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
pub enum StepState {
    Pending,
    MovedToTemp,
    Completed,
    RolledBack,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct JournalEntry {
    pub id: String,
    pub timestamp: i64,
    pub steps: Vec<JournalStep>,
}

pub struct OperationJournal {
    journal_path: PathBuf,
}

impl OperationJournal {
    pub fn new(journal_path: impl Into<PathBuf>) -> Self {
        Self {
            journal_path: journal_path.into(),
        }
    }

    pub fn save(&self, entry: &JournalEntry) -> Result<(), CoreError> {
        let json = serde_json::to_string_pretty(entry)
            .map_err(|e| CoreError::Journal(e.to_string()))?;
        if let Some(parent) = self.journal_path.parent() {
            fs::create_dir_all(parent)?;
        }
        let mut file = OpenOptions::new()
            .create(true)
            .write(true)
            .truncate(true)
            .open(&self.journal_path)?;
        file.write_all(json.as_bytes())?;
        file.flush()?;
        Ok(())
    }

    pub fn load(&self) -> Result<Option<JournalEntry>, CoreError> {
        if !self.journal_path.exists() {
            return Ok(None);
        }
        let mut file = fs::File::open(&self.journal_path)?;
        let mut content = String::new();
        file.read_to_string(&mut content)?;
        if content.trim().is_empty() {
            return Ok(None);
        }
        let entry: JournalEntry = serde_json::from_str(&content)
            .map_err(|e| CoreError::Journal(e.to_string()))?;
        Ok(Some(entry))
    }

    pub fn clear(&self) -> Result<(), CoreError> {
        if self.journal_path.exists() {
            fs::remove_file(&self.journal_path)?;
        }
        Ok(())
    }

    /// Khôi phục trạng thái nếu phiên làm việc trước bị ngắt đột ngột
    pub fn rollback_uncompleted(&self) -> Result<usize, CoreError> {
        let Some(mut entry) = self.load()? else {
            return Ok(0);
        };

        let mut restored = 0;
        for step in entry.steps.iter_mut().rev() {
            match step.state {
                StepState::Completed => {
                    if step.target.exists() && !step.original.exists() {
                        if let Err(e) = fs::rename(&step.target, &step.original) {
                            tracing::error!("Rollback failed from target: {:?}", e);
                        } else {
                            step.state = StepState::RolledBack;
                            restored += 1;
                        }
                    }
                }
                StepState::MovedToTemp => {
                    if step.temp.exists() && !step.original.exists() {
                        if let Err(e) = fs::rename(&step.temp, &step.original) {
                            tracing::error!("Rollback failed from temp: {:?}", e);
                        } else {
                            step.state = StepState::RolledBack;
                            restored += 1;
                        }
                    }
                }
                StepState::Pending | StepState::RolledBack => {}
            }
        }

        self.clear()?;
        Ok(restored)
    }
}
