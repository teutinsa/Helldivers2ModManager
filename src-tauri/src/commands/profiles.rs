use std::path::Path;

use anyhow_tauri::{IntoTAResult, TAResult};
use indexmap::IndexMap;

use crate::models::profile::{Profile, ProfilesConfig};

const PROFILES_FILE: &'static str = "profiles.json";

#[tauri::command]
pub async fn load_profiles() -> TAResult<ProfilesConfig> {
    let profiles_file = Path::new(PROFILES_FILE);

    if profiles_file.try_exists().into_ta_result()? {
        let data = tokio::fs::read(profiles_file).await.into_ta_result()?;
        serde_json::from_slice(&data).into_ta_result()
    } else {
        Ok(ProfilesConfig {
            profiles: vec![
                Profile::V1 {
                    name: "Default".to_string(),
                    configs: IndexMap::new()
                }
            ],
            active: 0
        })
    }
}

#[tauri::command]
pub async fn save_profiles(config: ProfilesConfig) -> TAResult<()> {
    let data = serde_json::to_vec_pretty(&config).into_ta_result()?;
    tokio::fs::write(PROFILES_FILE, data).await.into_ta_result()?;
    Ok(())
}