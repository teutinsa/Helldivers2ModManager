use std::path::{Path, PathBuf};

use serde::{Deserialize, Serialize};

mod ascii_string {
    use serde::{Deserializer, Serializer};

    use super::*;

    pub fn serialize<S: Serializer>(bytes: &[[u8; 16]], serializer: S) -> Result<S::Ok, S::Error> {
        let strings: Vec<&str> = bytes.iter()
            .map(|arr| str::from_utf8(arr).map_err(serde::ser::Error::custom))
            .collect::<Result<_, _>>()?;
        strings.serialize(serializer)
    }

    pub fn deserialize<'de, D: Deserializer<'de>>(deserializer: D) -> Result<Vec<[u8; 16]>, D::Error> {
        let strings = Vec::<String>::deserialize(deserializer)?;
        let mut result = Vec::with_capacity(strings.len());
        for s in strings {
            let bytes = s.as_bytes();
            if bytes.len() != 16 {
                return Err(serde::de::Error::custom("expected 16 characters"));
            }
            let mut arr = [0u8; 16];
            arr.copy_from_slice(bytes);
            result.push(arr);
        }
        Ok(result)
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(tag = "Version", rename_all = "PascalCase")]
pub enum Settings {
    #[serde(rename_all = "PascalCase")]
    V1 {
        game_path: PathBuf,
        #[serde(with = "ascii_string")]
        skip_list: Vec<[u8; 16]>
    }
}

impl Settings {
    pub async fn validate(&self) -> anyhow::Result<()> {
        match self {
            Settings::V1 { game_path, .. } => {
                if game_path.as_os_str().is_empty() {
                    anyhow::bail!("`game_path` is empty");
                }
                
                if !tokio::fs::try_exists(game_path).await.unwrap_or(false) {
                    anyhow::bail!("`game_path` doesn't exist");
                } else {
                    if !tokio::fs::try_exists(game_path.join("tools")).await.unwrap_or(false) {
                        anyhow::bail!("`game_path` doesn't contain dir \"tools\"");
                    }
                    if !tokio::fs::try_exists(game_path.join("data")).await.unwrap_or(false) {
                        anyhow::bail!("`game_path` doesn't contain dir \"data\"");
                    }
                    let bin_path = game_path.join("bin");
                    if !tokio::fs::try_exists(&bin_path).await.unwrap_or(false) {
                        anyhow::bail!("`game_path` doesn't contain dir \"bin\"");
                    } else {
                        if !tokio::fs::try_exists(bin_path.join("helldivers2.exe")).await.unwrap_or(false) {
                            anyhow::bail!("\"bin\" dir does not contain \"helldivers2.exe\"");
                        }
                    }
                }
                
                Ok(())
            },
        }
    }

    pub fn game_path(&self) -> &Path {
        match self {
            Settings::V1 { game_path, .. } => {
                game_path.as_path()
            },
        }
    }

    pub fn has_skip_entry(&self, s: &str) -> bool {
        match self {
            Settings::V1 { skip_list, .. } => {
                skip_list.iter()
                    .filter_map(|entry| {
                        str::from_utf8(entry).ok()
                    })
                    .any(|entry| entry == s)
            },
        }
    }
}