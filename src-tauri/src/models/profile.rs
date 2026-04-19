use serde::{Deserialize, Serialize};
use uuid::Uuid;

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "PascalCase", tag = "For")]
pub enum Config {
    #[serde(rename_all = "PascalCase")]
    Legacy {
        guid: Uuid,
        enabled: bool,
        selected: usize,
    },
    #[serde(rename_all = "PascalCase")]
    V1 {
        guid: Uuid,
        enabled: bool,
        toggled: Vec<bool>,
        selected: Vec<usize>,
    },
    #[serde(rename_all = "PascalCase")]
    V2 {
        guid: Uuid,
        enabled: bool,
        toggled: Vec<bool>,
        selected: Vec<usize>,
    }
}

impl Config {
    pub fn uuid(&self) -> &Uuid {
        match self {
            Config::Legacy { guid, .. } => guid,
            Config::V1 { guid, .. } => guid,
            Config::V2 { guid, .. } => guid,
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "PascalCase", tag = "Version")]
pub enum Profile {
    #[serde(rename_all = "PascalCase")]
    V1 {
        name: String,
        configs: Vec<Config>,
    }
}

impl Profile {
    pub fn new(name: &str) -> Self {
        Profile::V1 {
            name: name.to_string(),
            configs: Vec::new()
        }
    }

    pub fn name(&self) -> &str {
        match self {
            Profile::V1 { name, .. } => name,
        }
    }
    
    pub fn configs(&self) -> &[Config] {
        match self {
            Profile::V1 { configs, .. } => configs.as_slice(),
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "PascalCase")]
pub struct ProfilesConfig {
    pub profiles: Vec<Profile>,
    pub active: u64,
}