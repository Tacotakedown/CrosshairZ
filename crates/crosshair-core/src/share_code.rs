use std::io::{Read, Write};

use base64::{Engine as _, engine::general_purpose::URL_SAFE_NO_PAD};
use flate2::{Compression, read::ZlibDecoder, write::ZlibEncoder};
use thiserror::Error;

use crate::CrosshairData;

const PREFIX: &str = "chz1:";

#[derive(Debug, Error)]
pub enum ShareCodeError {
    #[error("share code prefix is invalid")]
    InvalidPrefix,
    #[error("base64 decode failed")]
    Base64,
    #[error("decompression failed")]
    Decompress,
    #[error("serialization failed")]
    Serialize,
    #[error("deserialization failed")]
    Deserialize,
}

pub fn encode_share_code(data: &CrosshairData) -> Result<String, ShareCodeError> {
    let json = serde_json::to_vec(data).map_err(|_| ShareCodeError::Serialize)?;

    let mut encoder = ZlibEncoder::new(Vec::new(), Compression::default());
    encoder
        .write_all(&json)
        .map_err(|_| ShareCodeError::Serialize)?;
    let compressed = encoder.finish().map_err(|_| ShareCodeError::Serialize)?;

    Ok(format!("{PREFIX}{}", URL_SAFE_NO_PAD.encode(compressed)))
}

pub fn decode_share_code(code: &str) -> Result<CrosshairData, ShareCodeError> {
    let payload = code
        .strip_prefix(PREFIX)
        .ok_or(ShareCodeError::InvalidPrefix)?;
    let bytes = URL_SAFE_NO_PAD
        .decode(payload)
        .map_err(|_| ShareCodeError::Base64)?;

    let mut decoder = ZlibDecoder::new(bytes.as_slice());
    let mut json = Vec::new();
    decoder
        .read_to_end(&mut json)
        .map_err(|_| ShareCodeError::Decompress)?;

    serde_json::from_slice(&json).map_err(|_| ShareCodeError::Deserialize)
}
