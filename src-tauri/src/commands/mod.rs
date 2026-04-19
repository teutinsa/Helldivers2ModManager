use anyhow_tauri::TAResult;
use tauri::State;
use uuid::Uuid;

use crate::{AppState, models::profile::Config};

pub mod mods;
pub mod profiles;

#[tauri::command]
pub async fn deploy(state: State<'_, AppState>, configs: Vec<Config>) -> TAResult<()> {
    todo!()
}