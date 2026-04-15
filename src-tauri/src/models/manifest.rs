use serde::{Deserialize, Serialize};

pub mod legacy {
    use std::path::PathBuf;
    use serde::{Deserialize, Serialize};
    use uuid::Uuid;

    #[derive(Debug, Clone, Serialize, Deserialize)]
    #[serde(rename_all = "PascalCase")]
    pub struct Manifest {
        pub guid: Uuid,
        pub name: String,
        pub description: String,
        pub icon_path: Option<PathBuf>,
        pub options: Option<Vec<String>>,
    }
}

pub mod v1 {
    use std::path::PathBuf;
    use serde::{Deserialize, Serialize};
    use uuid::Uuid;
    use crate::models::Version;

    type Optional<T> = std::option::Option<T>;
    
    #[derive(Debug, Clone, Serialize, Deserialize)]
    #[serde(rename_all = "PascalCase")]
    pub struct Manifest {
        pub version: Version<1>,
        pub guid: Uuid,
        pub name: String,
        pub description: String,
        pub icon_path: Optional<PathBuf>,
        pub options: Optional<Vec<Option>>,
        pub nexus_data: Optional<NexusData>,
    }
    
    #[derive(Debug, Clone, Serialize, Deserialize)]
    #[serde(rename_all = "PascalCase")]
    pub struct Option {
        pub name: String,
        pub description: String,
        pub include: Optional<Vec<PathBuf>>,
        pub image: Optional<PathBuf>,
        pub sub_options: Optional<Vec<SubOption>>,
    }
    
    #[derive(Debug, Clone, Serialize, Deserialize)]
    #[serde(rename_all = "PascalCase")]
    pub struct SubOption {
        pub name: String,
        pub description: String,
        pub include: Vec<PathBuf>,
        pub image: Optional<PathBuf>,
    }
    
    #[derive(Debug, Clone, Serialize, Deserialize)]
    #[serde(rename_all = "PascalCase")]
    pub struct NexusData {
        pub mod_id: u64,
        pub version: String,
    }
}

pub mod v2 {
    use std::path::PathBuf;
    use serde::{Deserialize, Serialize};
    use uuid::Uuid;
    use crate::models::Version;

    type Optional<T> = std::option::Option<T>;

    #[derive(Debug, Clone, Serialize, Deserialize)]
    #[serde(rename_all = "PascalCase")]
    pub struct Manifest {
        pub version: Version<2>,
        pub guid: Uuid,
        pub name: String,
        pub description: String,
        pub icon_path: Optional<PathBuf>,
        pub options: Optional<Vec<Option>>,
        pub categories: Optional<Vec<Category>>,
        pub tags: Optional<Vec<String>>,
        pub nexus_data: Optional<NexusData>,
    }

    #[derive(Debug, Clone, Serialize, Deserialize)]
    #[serde(rename_all = "PascalCase")]
    pub struct Option {
        pub guid: Uuid,
        pub name: String,
        pub category_ref: Optional<Uuid>,
        pub description: String,
        pub include: Optional<Vec<PathBuf>>,
        pub image: Optional<PathBuf>,
        pub sub_options: Optional<Vec<SubOption>>,
    }

    #[derive(Debug, Clone, Serialize, Deserialize)]
    #[serde(rename_all = "PascalCase")]
    pub struct SubOption {
        pub guid: Uuid,
        pub name: String,
        pub description: String,
        pub include: Vec<PathBuf>,
        pub image: Optional<PathBuf>,
    }

    #[derive(Debug, Clone, Serialize, Deserialize)]
    #[serde(rename_all = "PascalCase")]
    pub struct Category {
        pub guid: Uuid,
        pub name: String,
        pub description: String,
    }

    #[derive(Debug, Clone, Serialize, Deserialize)]
    #[serde(rename_all = "PascalCase")]
    pub struct NexusData {
        pub mod_id: u64
    }
}

#[derive(Debug, Clone, Serialize)]
#[serde(untagged)]
pub enum Manifest {
    Legacy(legacy::Manifest),
    V1(v1::Manifest),
    V2(v2::Manifest),
}

impl<'de> Deserialize<'de> for Manifest {
    fn deserialize<D: serde::Deserializer<'de>>(deserializer: D) -> Result<Self, D::Error> {
        use serde::de::Error;

        let raw = serde_json::Value::deserialize(deserializer)?;

        if let Some(version) = raw.get("Version").and_then(|v| v.as_u64()) {
            match version {
                1 => {
                    v1::Manifest::deserialize(raw)
                        .map(Manifest::V1)
                        .map_err(|e| Error::custom(format!("v1 manifest deserialization failed: {}", e)))
                }
                2 => {
                    v2::Manifest::deserialize(raw)
                        .map(Manifest::V2)
                        .map_err(|e| Error::custom(format!("v2 manifest deserialization failed: {}", e)))
                }
                v => Err(Error::custom(format!("unknown manifest version {}", v)))
            }
        } else {
            legacy::Manifest::deserialize(raw)
                .map(Manifest::Legacy)
                .map_err(|e| Error::custom(format!("legacy manifest deserialization failed: {}", e)))
        }
    }
}