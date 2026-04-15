use std::path::PathBuf;

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
    V1 {
        game_path: PathBuf,
        #[serde(with = "ascii_string")]
        skip_list: Vec<[u8; 16]>
    }
}