pub mod color;
pub mod draw;
pub mod model;
pub mod share_code;
pub mod validation;

pub use color::Rgba8;
pub use draw::{CircleCmd, DrawCmd, FilledRectCmd, FilledTriangleCmd, LineCmd, StrokeCircleCmd, StrokeRectCmd, build_draw_cmds};
pub use model::{CrosshairData, CrosshairProfile, CrosshairStyle, CustomShape, CustomShapeKind, ProfilesFile};
pub use share_code::{ShareCodeError, decode_share_code, encode_share_code};
pub use validation::{ValidatedCrosshair, ValidationError};

#[cfg(test)]
mod tests {
    use crate::{
        CrosshairData, CrosshairStyle, ValidatedCrosshair, decode_share_code,
        draw::build_draw_cmds, encode_share_code,
    };

    #[test]
    fn share_code_round_trip() {
        let data = CrosshairData::default();
        let code = encode_share_code(&data).unwrap();
        let decoded = decode_share_code(&code).unwrap();

        assert_eq!(data.width, decoded.width);
        assert_eq!(data.height, decoded.height);
        assert_eq!(data.color.r, decoded.color.r);
    }

    #[test]
    fn draw_cmds_exist_for_default_crosshair() {
        let validated = ValidatedCrosshair::new(CrosshairData::default()).unwrap();
        let cmds = build_draw_cmds(&validated, 960.0, 540.0);
        assert!(!cmds.is_empty());
    }

    #[test]
    fn circle_style_produces_stroke_circle() {
        use crate::draw::DrawCmd;
        let data = CrosshairData { style: CrosshairStyle::Circle, ..CrosshairData::default() };
        let validated = ValidatedCrosshair::new(data).unwrap();
        let cmds = build_draw_cmds(&validated, 100.0, 100.0);
        assert!(cmds.iter().any(|c| matches!(c, DrawCmd::StrokeCircle(_))));
    }

    #[test]
    fn diagonal_x_emits_lines() {
        use crate::draw::DrawCmd;
        let data = CrosshairData { style: CrosshairStyle::DiagonalX, ..CrosshairData::default() };
        let validated = ValidatedCrosshair::new(data).unwrap();
        let cmds = build_draw_cmds(&validated, 100.0, 100.0);
        assert!(cmds.iter().any(|c| matches!(c, DrawCmd::Line(_))));
    }
}
