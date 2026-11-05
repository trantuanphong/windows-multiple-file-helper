use std::path::{Path, PathBuf};
use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
pub enum SortMode {
    NameAsc,
    NameDesc,
    DateCreatedAsc,
    DateCreatedDesc,
    DateModifiedAsc,
    DateModifiedDesc,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
pub enum NumberingPosition {
    Prefix,
    Suffix,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct NumberingConfig {
    pub start: u64,
    pub step: u64,
    pub digits: usize,
    pub prefix: String,
    pub suffix: String,
    pub separator: String,
    pub position: NumberingPosition,
    pub keep_original_name: bool,
}

impl Default for NumberingConfig {
    fn default() -> Self {
        Self {
            start: 1,
            step: 1,
            digits: 3,
            prefix: String::new(),
            suffix: String::new(),
            separator: "_".to_string(),
            position: NumberingPosition::Prefix,
            keep_original_name: true,
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct TrimConfig {
    pub count: usize,
    pub trim_separators: bool,
}

impl Default for TrimConfig {
    fn default() -> Self {
        Self {
            count: 0,
            trim_separators: true,
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ReplaceConfig {
    pub search: String,
    pub replace: String,
    pub match_case: bool,
}

impl Default for ReplaceConfig {
    fn default() -> Self {
        Self {
            search: String::new(),
            replace: String::new(),
            match_case: false,
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct RenameItem {
    pub original_path: PathBuf,
    pub new_name: String,
    pub new_path: PathBuf,
}

impl RenameItem {
    pub fn new(original_path: impl Into<PathBuf>, new_name: String) -> Self {
        let original_path = original_path.into();
        let parent = original_path.parent().unwrap_or_else(|| Path::new(""));
        let new_path = parent.join(&new_name);
        Self {
            original_path,
            new_name,
            new_path,
        }
    }

    pub fn is_changed(&self) -> bool {
        self.original_path != self.new_path
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct RenamePlan {
    pub from: PathBuf,
    pub to: PathBuf,
}
