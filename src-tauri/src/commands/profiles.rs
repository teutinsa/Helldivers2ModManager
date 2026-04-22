use anyhow_tauri::{IntoTAResult, TAResult};
use tauri::State;

use crate::{AppState, models::profile::{Profile, ProfilesConfig}};

const PROFILES_FILE: &'static str = "profiles.json";

#[tauri::command]
pub async fn load_profiles(state: State<'_, AppState>) -> TAResult<ProfilesConfig> {
    let profiles_file = state.base_path.join(PROFILES_FILE);

    if tokio::fs::try_exists(&profiles_file).await.into_ta_result()? {
        let data = tokio::fs::read(profiles_file).await.into_ta_result()?;
        serde_json::from_slice(&data).into_ta_result()
    } else {
        Ok(ProfilesConfig {
            profiles: vec![
                Profile::V1 {
                    name: "Default".to_string(),
                    configs: Vec::new()
                }
            ],
            active: 0
        })
    }
}

#[tauri::command]
pub async fn save_profiles(state: State<'_, AppState>, config: ProfilesConfig) -> TAResult<()> {
    log::info!("Saving profiles...");
    
    for profile in &config.profiles {
        log::debug!("Profile \"{}\" Order:", profile.name());
        for config in profile.configs() {
            log::debug!("- {{{}}}", config.uuid());
        }
    }

    let data = serde_json::to_vec_pretty(&config).into_ta_result()?;
    tokio::fs::write(state.base_path.join(PROFILES_FILE), data).await.into_ta_result()?;
    
    log::info!("Profiles saved.");

    Ok(())
}