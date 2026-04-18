pub mod commands;
pub mod models;
pub mod archive;

use std::path::PathBuf;

use log::LevelFilter;
use tauri_plugin_log::{Target, TargetKind};
use tokio::sync::Mutex;

use crate::models::Mod;

pub struct AppState {
    base_path: PathBuf,
    mods: Mutex<Option<Vec<Mod>>>,
}

impl AppState {
    pub fn new(base_path: PathBuf) -> Self {
        Self {
            base_path,
            mods: Mutex::default(),
        }
    }
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let exe_dir = std::env::current_exe()
        .unwrap()
        .parent()
        .unwrap()
        .to_path_buf();

    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_dialog::init())
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
                    //#[cfg(not(debug_assertions))]
                    Target::new(TargetKind::Folder {
                        path: exe_dir.clone(),
                        file_name: None
                    })
                ])
                .build()
        )
        .manage(AppState::new(exe_dir))
        .invoke_handler(tauri::generate_handler![
            commands::mods::get_mods,
            commands::mods::add_mod,
            commands::mods::add_mods,
            commands::profiles::load_profiles,
            commands::profiles::save_profiles
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
