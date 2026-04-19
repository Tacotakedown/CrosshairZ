use serde::{Deserialize, Serialize};

use crate::Rgba8;

#[derive(Debug, Clone, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "snake_case")]
pub enum CrosshairStyle {
    /// Traditional plus-sign cross with 4 arms.
    Classic,
    /// Circle / ring (uses circle_radius + circle_thickness).
    Circle,
    /// Center dot only.
    Dot,
    /// X-shape — Classic rotated 45°.
    DiagonalX,
    /// Hollow square box.
    Square,
    /// Circle combined with Classic arms.
    CircleCross,
    /// User-defined shapes drawn in the built-in editor.
    Custom,
}

#[derive(Debug, Clone, PartialEq, Serialize, Deserialize)]
#[serde(tag = "type", rename_all = "snake_case")]
pub enum CustomShapeKind {
    Line {
        x1: f32,
        y1: f32,
        x2: f32,
        y2: f32,
    },
    Circle {
        cx: f32,
        cy: f32,
        radius: f32,
        filled: bool,
    },
    Rect {
        x: f32,
        y: f32,
        w: f32,
        h: f32,
        filled: bool,
    },
    Triangle {
        x1: f32,
        y1: f32,
        x2: f32,
        y2: f32,
        x3: f32,
        y3: f32,
        filled: bool,
    },
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CustomShape {
    pub kind: CustomShapeKind,
    pub color: Rgba8,
    pub thickness: f32,
    pub outline_enabled: bool,
    pub outline_thickness: f32,
    pub outline_color: Rgba8,
}

impl Default for CrosshairStyle {
    fn default() -> Self {
        CrosshairStyle::Classic
    }
}

fn default_style() -> CrosshairStyle {
    CrosshairStyle::Classic
}
fn default_true() -> bool {
    true
}
fn default_width() -> f32 {
    18.0
}
fn default_height() -> f32 {
    18.0
}
fn default_thickness() -> f32 {
    2.0
}
fn default_gap() -> f32 {
    6.0
}
fn default_dot_radius() -> f32 {
    1.5
}
fn default_opacity() -> f32 {
    1.0
}
fn default_outline_thickness() -> f32 {
    1.0
}
fn default_white() -> Rgba8 {
    Rgba8::WHITE
}
fn default_black() -> Rgba8 {
    Rgba8::BLACK
}
fn default_inner_lines_length() -> f32 {
    4.0
}
fn default_inner_lines_thickness() -> f32 {
    2.0
}
fn default_inner_lines_gap() -> f32 {
    3.0
}
fn default_circle_radius() -> f32 {
    24.0
}
fn default_circle_thickness() -> f32 {
    2.0
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ProfilesFile {
    pub version: u32,
    pub profiles: Vec<CrosshairProfile>,
    pub active_profile_id: Option<String>,
}

impl Default for ProfilesFile {
    fn default() -> Self {
        Self {
            version: 2,
            profiles: vec![CrosshairProfile::default()],
            active_profile_id: None,
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CrosshairProfile {
    pub id: String,
    pub name: String,
    pub crosshair: CrosshairData,
}

impl Default for CrosshairProfile {
    fn default() -> Self {
        Self {
            id: "default".to_string(),
            name: "Default".to_string(),
            crosshair: CrosshairData::default(),
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CrosshairData {
    #[serde(default = "default_true")]
    pub enabled: bool,

    #[serde(default = "default_style")]
    pub style: CrosshairStyle,

    #[serde(default = "default_true")]
    pub show_top: bool,
    #[serde(default = "default_true")]
    pub show_bottom: bool,
    #[serde(default = "default_true")]
    pub show_left: bool,
    #[serde(default = "default_true")]
    pub show_right: bool,
    #[serde(default = "default_true")]
    pub show_center_dot: bool,

    #[serde(default = "default_width")]
    pub width: f32,
    #[serde(default = "default_height")]
    pub height: f32,
    #[serde(default = "default_thickness")]
    pub thickness: f32,
    #[serde(default = "default_gap")]
    pub gap: f32,
    #[serde(default = "default_dot_radius")]
    pub center_dot_radius: f32,

    #[serde(default = "default_opacity")]
    pub opacity: f32,
    #[serde(default = "default_white")]
    pub color: Rgba8,
    #[serde(default = "default_true")]
    pub outline_enabled: bool,
    #[serde(default = "default_outline_thickness")]
    pub outline_thickness: f32,
    #[serde(default = "default_black")]
    pub outline_color: Rgba8,

    /// Static rotation offset in degrees.
    #[serde(default)]
    pub rotation: f32,
    /// Animation speed in degrees/second (0 = static).
    #[serde(default)]
    pub rotation_speed: f32,

    #[serde(default)]
    pub inner_lines_enabled: bool,
    #[serde(default = "default_inner_lines_length")]
    pub inner_lines_length: f32,
    #[serde(default = "default_inner_lines_thickness")]
    pub inner_lines_thickness: f32,
    #[serde(default = "default_inner_lines_gap")]
    pub inner_lines_gap: f32,

    #[serde(default = "default_circle_radius")]
    pub circle_radius: f32,
    #[serde(default = "default_circle_thickness")]
    pub circle_thickness: f32,

    #[serde(default)]
    pub custom_shapes: Vec<CustomShape>,
}

impl Default for CrosshairData {
    fn default() -> Self {
        Self {
            enabled: true,
            style: CrosshairStyle::Classic,
            show_top: true,
            show_bottom: true,
            show_left: true,
            show_right: true,
            show_center_dot: true,
            width: 18.0,
            height: 18.0,
            thickness: 2.0,
            gap: 6.0,
            center_dot_radius: 1.5,
            opacity: 1.0,
            color: Rgba8::WHITE,
            outline_enabled: true,
            outline_thickness: 1.0,
            outline_color: Rgba8::BLACK,
            rotation: 0.0,
            rotation_speed: 0.0,
            inner_lines_enabled: false,
            inner_lines_length: 4.0,
            inner_lines_thickness: 2.0,
            inner_lines_gap: 3.0,
            circle_radius: 24.0,
            circle_thickness: 2.0,
            custom_shapes: Vec::new(),
        }
    }
}
