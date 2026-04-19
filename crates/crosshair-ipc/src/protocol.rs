use serde::{Deserialize, Serialize};

use crosshair_core::{CrosshairData, DrawCmd};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum Request {
    GetActiveProfile,
    SetActiveCrosshair(CrosshairData),
    BuildPreview {
        crosshair: CrosshairData,
        width: f32,
        height: f32,
    },
    EncodeShareCode(CrosshairData),
    DecodeShareCode(String),
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum Response {
    ActiveProfile(CrosshairData),
    Preview(Vec<DrawCmd>),
    ShareCode(String),
    Decoded(CrosshairData),
    Error(String),
}
