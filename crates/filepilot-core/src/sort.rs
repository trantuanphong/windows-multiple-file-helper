use crate::model::SortMode;
use std::fs;
use std::path::PathBuf;

pub fn sort_paths(paths: &mut [PathBuf], mode: SortMode) {
    match mode {
        SortMode::NameAsc => {
            paths.sort_by(|a, b| {
                let name_a = a.file_name().and_then(|s| s.to_str()).unwrap_or("");
                let name_b = b.file_name().and_then(|s| s.to_str()).unwrap_or("");
                natord::compare(name_a, name_b)
            });
        }
        SortMode::NameDesc => {
            paths.sort_by(|a, b| {
                let name_a = a.file_name().and_then(|s| s.to_str()).unwrap_or("");
                let name_b = b.file_name().and_then(|s| s.to_str()).unwrap_or("");
                natord::compare(name_b, name_a)
            });
        }
        SortMode::DateCreatedAsc => {
            paths.sort_by(|a, b| {
                let ta = fs::metadata(a).and_then(|m| m.created()).ok();
                let tb = fs::metadata(b).and_then(|m| m.created()).ok();
                ta.cmp(&tb)
            });
        }
        SortMode::DateCreatedDesc => {
            paths.sort_by(|a, b| {
                let ta = fs::metadata(a).and_then(|m| m.created()).ok();
                let tb = fs::metadata(b).and_then(|m| m.created()).ok();
                tb.cmp(&ta)
            });
        }
        SortMode::DateModifiedAsc => {
            paths.sort_by(|a, b| {
                let ta = fs::metadata(a).and_then(|m| m.modified()).ok();
                let tb = fs::metadata(b).and_then(|m| m.modified()).ok();
                ta.cmp(&tb)
            });
        }
        SortMode::DateModifiedDesc => {
            paths.sort_by(|a, b| {
                let ta = fs::metadata(a).and_then(|m| m.modified()).ok();
                let tb = fs::metadata(b).and_then(|m| m.modified()).ok();
                tb.cmp(&ta)
            });
        }
    }
}
