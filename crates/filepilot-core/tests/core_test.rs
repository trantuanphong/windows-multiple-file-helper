#[cfg(test)]
mod tests {
    use filepilot_core::*;
    use std::fs::File;
    use std::path::PathBuf;

    #[test]
    fn test_numbering_plan() {
        let files = vec![
            PathBuf::from("test").join("photo_b.jpg"),
            PathBuf::from("test").join("photo_a.jpg"),
        ];
        let cfg = NumberingConfig {
            start: 1,
            step: 1,
            digits: 3,
            prefix: "IMG_".to_string(),
            suffix: "".to_string(),
            separator: "_".to_string(),
            position: NumberingPosition::Prefix,
            keep_original_name: true,
        };

        let plans = Renamer::plan_numbering(&files, &cfg);
        assert_eq!(plans.len(), 2);
        assert_eq!(plans[0].new_name, "IMG_001_photo_b.jpg");
        assert_eq!(plans[1].new_name, "IMG_002_photo_a.jpg");
    }

    #[test]
    fn test_trim_prefix_plan() {
        let files = vec![
            PathBuf::from("test").join("01 - document.pdf"),
            PathBuf::from("test").join("02_track.mp3"),
        ];
        let cfg = TrimConfig {
            count: 2,
            trim_separators: true,
        };

        let plans = Renamer::plan_trim_prefix(&files, &cfg);
        assert_eq!(plans.len(), 2);
        assert_eq!(plans[0].new_name, "document.pdf");
        assert_eq!(plans[1].new_name, "track.mp3");
    }

    #[test]
    fn test_two_phase_rename_cycle_and_rollback() {
        let temp_dir = std::env::temp_dir().join(format!("filepilot_test_{}", std::time::SystemTime::now().duration_since(std::time::UNIX_EPOCH).unwrap().as_millis()));
        std::fs::create_dir_all(&temp_dir).unwrap();

        let file_a = temp_dir.join("a.txt");
        let file_b = temp_dir.join("b.txt");
        File::create(&file_a).unwrap();
        File::create(&file_b).unwrap();

        let target_a = temp_dir.join("b_new.txt");
        let target_b = temp_dir.join("a_new.txt");

        let plans = vec![
            RenamePlan { from: file_a.clone(), to: target_a.clone() },
            RenamePlan { from: file_b.clone(), to: target_b.clone() },
        ];

        let journal = OperationJournal::new(temp_dir.join("journal.json"));
        let res = Renamer::execute_plans(&plans, &journal);
        assert!(res.is_ok());
        assert_eq!(res.unwrap(), 2);
        assert!(target_a.exists());
        assert!(target_b.exists());
        assert!(!file_a.exists());
        assert!(!file_b.exists());

        std::fs::remove_dir_all(&temp_dir).ok();
    }
}
