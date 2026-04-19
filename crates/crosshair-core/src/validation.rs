use thiserror::Error;

use crate::CrosshairData;

#[derive(Debug, Clone)]
pub struct ValidatedCrosshair(pub CrosshairData);

#[derive(Debug, Error)]
pub enum ValidationError {
    #[error("crosshair dimensions must be finite numbers")]
    NonFinite,
}

impl ValidatedCrosshair {
    pub fn new(mut value: CrosshairData) -> Result<Self, ValidationError> {
        let floats = [
            value.width,
            value.height,
            value.thickness,
            value.gap,
            value.center_dot_radius,
            value.opacity,
            value.outline_thickness,
            value.rotation,
            value.rotation_speed,
            value.inner_lines_length,
            value.inner_lines_thickness,
            value.inner_lines_gap,
            value.circle_radius,
            value.circle_thickness,
        ];

        if floats.iter().any(|v| !v.is_finite()) {
            return Err(ValidationError::NonFinite);
        }

        value.width = value.width.clamp(0.0, 256.0);
        value.height = value.height.clamp(0.0, 256.0);
        value.thickness = value.thickness.clamp(0.5, 64.0);
        value.gap = value.gap.clamp(0.0, 128.0);
        value.center_dot_radius = value.center_dot_radius.clamp(0.0, 64.0);
        value.opacity = value.opacity.clamp(0.0, 1.0);
        value.outline_thickness = value.outline_thickness.clamp(0.0, 32.0);
        value.rotation_speed = value.rotation_speed.clamp(0.0, 720.0);
        value.inner_lines_length = value.inner_lines_length.clamp(0.0, 64.0);
        value.inner_lines_thickness = value.inner_lines_thickness.clamp(0.5, 24.0);
        value.inner_lines_gap = value.inner_lines_gap.clamp(0.0, 128.0);
        value.circle_radius = value.circle_radius.clamp(1.0, 256.0);
        value.circle_thickness = value.circle_thickness.clamp(0.5, 64.0);

        Ok(Self(value))
    }

    pub fn into_inner(self) -> CrosshairData {
        self.0
    }
}
