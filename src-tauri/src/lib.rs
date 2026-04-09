pub mod commands;
pub mod services;

use std::sync::Mutex;
use log::LevelFilter;
use tauri_plugin_log::{Target, TargetKind};
use services::{mods::ModsService, profiles::ProfilesService};

#[derive(Debug, Default)]
pub struct AppState {
    pub mods: Mutex<ModsService>,
    pub profiles: Mutex<ProfilesService>
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
            
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
