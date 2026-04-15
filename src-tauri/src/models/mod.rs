pub mod manifest;
pub mod profile;
pub mod settings;

use std::path::PathBuf;
use serde::{Deserialize, Serialize};
use uuid::Uuid;
use manifest::Manifest;

#[derive(Debug, Clone, Copy)]
pub struct Version<const N: u64>;

impl<const N: u64> Serialize for Version<N> {
    fn serialize<S: serde::Serializer>(&self, serializer: S) -> Result<S::Ok, S::Error> {
        serializer.serialize_u64(N)
    }
}

impl<'de, const N: u64> Deserialize<'de> for Version<N> {
    fn deserialize<D: serde::Deserializer<'de>>(deserializer: D) -> Result<Self, D::Error> {
        struct Visitor<const N: u64>;

        impl<'de, const N: u64> serde::de::Visitor<'de> for Visitor<N> {
            type Value = Version<N>;
        
            fn expecting(&self, formatter: &mut std::fmt::Formatter) -> std::fmt::Result {
                formatter.write_fmt(format_args!("expected version number {}", N))
            }

            fn visit_i64<E: serde::de::Error>(self, v: i64) -> Result<Self::Value, E>  {
                u64::try_from(v)
                    .map_err(|_| E::custom("version number must be none-negative"))
                    .and_then(|v| self.visit_u64(v))
            }

            fn visit_u64<E: serde::de::Error>(self, v: u64) -> Result<Self::Value, E> {
                if v == N {
                    Ok(Version)
                } else {
                    Err(E::custom(format!("expected version number {}", N)))
                }
            }

            fn visit_u128<E: serde::de::Error>(self, v: u128) -> Result<Self::Value, E> {
                u64::try_from(v)
                    .map_err(|_| E::custom("version number out of range"))
                    .and_then(|v| self.visit_u64(v))
            }
        }

        deserializer.deserialize_u64(Visitor)
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "PascalCase")]
pub struct Mod {
    pub manifest: Manifest,
    pub directory: PathBuf,
}

impl Mod {
    pub fn guid(&self) -> Uuid {
        match &self.manifest {
            Manifest::Legacy(m) => m.guid,
            Manifest::V1(m) => m.guid,
            Manifest::V2(m) => m.guid,
        }
    }
}