use serde::{Deserialize, Serialize};
use uuid::Uuid;
use indexmap::{IndexMap, map::Slice};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "PascalCase", tag = "For")]
pub enum Config {
    #[serde(rename_all = "PascalCase")]
    Legacy {
        enabled: bool,
        selected: usize,
    },
    #[serde(rename_all = "PascalCase")]
    V1 {
        enabled: bool,
        toggled: Vec<bool>,
        selected: Vec<usize>,
    },
    #[serde(rename_all = "PascalCase")]
    V2 {
        enabled: bool,
        toggled: Vec<bool>,
        selected: Vec<usize>,
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "PascalCase", tag = "Version")]
pub enum Profile {
    #[serde(rename_all = "PascalCase")]
    V1 {
        name: String,
        configs: IndexMap<Uuid, Config>,
    }
}

impl Profile {
    pub fn new(name: &str) -> Self {
        Profile::V1 {
            name: name.to_string(),
            configs: IndexMap::new()
        }
    }

    pub fn name(&self) -> &str {
        match self {
            Profile::V1 { name, .. } => name,
        }
    }
    
    pub fn configs(&self) -> &Slice<Uuid, Config> {
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