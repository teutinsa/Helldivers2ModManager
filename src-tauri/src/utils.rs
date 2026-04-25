use std::path::{Path, PathBuf};

pub async fn fix_path_casing(base: &Path, relative: &Path) -> anyhow::Result<PathBuf> {
    let mut current = base.to_path_buf();
    'components: for components in relative.components() {
        let component_str = components.as_os_str().to_string_lossy();
        
        let mut entries = tokio::fs::read_dir(&current).await?;
        while let Some(entry) = entries.next_entry().await? {
            if entry.file_name().to_string_lossy().to_lowercase() == component_str.to_lowercase() {
                current.push(entry.file_name());
                continue 'components;
            }
        }

        current.push(components.as_os_str());
    }

    Ok(current.strip_prefix(base)?.to_path_buf())
}