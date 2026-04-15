pub mod commands;
pub mod models;
pub mod archive;

use log::LevelFilter;
use tauri_plugin_log::{Target, TargetKind};
use tokio::sync::Mutex;

use crate::models::Mod;

#[derive(Default)]
pub struct AppState {
    mods: Mutex<Option<Vec<Mod>>>,
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .plugin(
            tauri_plugin_log::Builder::new()
                .level(if cfg!(debug_assertions) {
                    LevelFilter::Debug
                } else {
                    LevelFilter::Info
                })
                .targets([
                    Target::new(TargetKind::Stdout),
                    Target::new(TargetKind::Webview),
                    Target::new(TargetKind::Folder {
                        path: "log/".into(),
                        file_name: None
                    })
                ])
                .build()
        )
        .manage(AppState::default())
        .invoke_handler(tauri::generate_handler![
            commands::mods::get_mods,
            commands::mods::add_mod,
            commands::profiles::load_profiles,
            commands::profiles::save_profiles,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
