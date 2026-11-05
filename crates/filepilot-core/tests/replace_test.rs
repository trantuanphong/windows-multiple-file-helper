use filepilot_core::*;
use std::fs::File;
use std::path::PathBuf;

#[test]
fn test_replace_plan() {
    let files = vec![
        PathBuf::from(r"C:\test\OldReport_2025_v1.docx"),
        PathBuf::from(r"C:\test\OLDREPORT_final.docx"),
    ];
    let cfg = ReplaceConfig {
        search: "OldReport".to_string(),
        replace: "NewBrief".to_string(),
        match_case: false,
    };

    let plans = Renamer::plan_replace(&files, &cfg);
    assert_eq!(plans.len(), 2);
    assert_eq!(plans[0].new_name, "NewBrief_2025_v1.docx");
    assert_eq!(plans[1].new_name, "NewBrief_final.docx");
}

#[test]
fn test_unicode_and_special_characters() {
    let temp_dir = std::env::temp_dir().join(format!("filepilot_unicode_{}", std::time::SystemTime::now().duration_since(std::time::UNIX_EPOCH).unwrap().as_millis()));
    std::fs::create_dir_all(&temp_dir).unwrap();

    let original = temp_dir.join("Tài liệu báo cáo tài chính #2026.pdf");
    File::create(&original).unwrap();

    let cfg = NumberingConfig {
        start: 1,
        step: 1,
        digits: 2,
        prefix: "Mục_".to_string(),
        suffix: "".to_string(),
        separator: "_".to_string(),
        position: NumberingPosition::Prefix,
        keep_original_name: true,
    };

    let items = Renamer::plan_numbering(&[original.clone()], &cfg);
    assert_eq!(items[0].new_name, "Mục_01_Tài liệu báo cáo tài chính #2026.pdf");

    let plans = vec![RenamePlan {
        from: original.clone(),
        to: items[0].new_path.clone(),
    }];

    let journal = OperationJournal::new(temp_dir.join("journal.json"));
    let res = Renamer::execute_plans(&plans, &journal);
    assert!(res.is_ok());
    assert!(items[0].new_path.exists());
    assert!(!original.exists());

    std::fs::remove_dir_all(&temp_dir).ok();
}
