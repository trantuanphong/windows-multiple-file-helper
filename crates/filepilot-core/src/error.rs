use thiserror::Error;

#[derive(Error, Debug)]
pub enum CoreError {
    #[error("File not found: {0}")]
    FileNotFound(String),

    #[error("Duplicate target path: {0}")]
    DuplicateTarget(String),

    #[error("Destination already exists: {0}")]
    DestinationExists(String),

    #[error("IO error: {0}")]
    Io(#[from] std::io::Error),

    #[error("Journal error: {0}")]
    Journal(String),

    #[error("Invalid operation: {0}")]
    InvalidOperation(String),
}
