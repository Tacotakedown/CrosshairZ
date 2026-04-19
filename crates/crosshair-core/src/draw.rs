use std::f32::consts::PI;

use serde::{Deserialize, Serialize};

use crate::{
    Rgba8, ValidatedCrosshair,
    model::{CrosshairStyle, CustomShapeKind},
};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum DrawCmd {
    Line(LineCmd),
    Circle(CircleCmd),
    StrokeCircle(StrokeCircleCmd),
    FilledRect(FilledRectCmd),
    StrokeRect(StrokeRectCmd),
    FilledTriangle(FilledTriangleCmd),
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct LineCmd {
    pub x1: f32,
    pub y1: f32,
    pub x2: f32,
    pub y2: f32,
    pub thickness: f32,
    pub color: Rgba8,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CircleCmd {
    pub x: f32,
    pub y: f32,
    pub radius: f32,
    pub color: Rgba8,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct StrokeCircleCmd {
    pub x: f32,
    pub y: f32,
    pub radius: f32,
    pub stroke_width: f32,
    pub color: Rgba8,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct FilledRectCmd {
    pub x: f32,
    pub y: f32,
    pub w: f32,
    pub h: f32,
    pub color: Rgba8,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct StrokeRectCmd {
    pub x: f32,
    pub y: f32,
    pub w: f32,
    pub h: f32,
    pub stroke_width: f32,
    pub color: Rgba8,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct FilledTriangleCmd {
    pub x1: f32,
    pub y1: f32,
    pub x2: f32,
    pub y2: f32,
    pub x3: f32,
    pub y3: f32,
    pub color: Rgba8,
}

pub fn build_draw_cmds(data: &ValidatedCrosshair, cx: f32, cy: f32) -> Vec<DrawCmd> {
    let d = &data.0;
    if !d.enabled {
        return Vec::new();
    }

    let color = with_opacity(d.color, d.opacity);
    let outline = with_opacity(d.outline_color, d.opacity);

    let mut out = Vec::new();

    match &d.style {
        CrosshairStyle::Classic => {
            push_cross(&mut out, d, cx, cy, color, outline, 0.0);
        }
        CrosshairStyle::DiagonalX => {
            push_cross(&mut out, d, cx, cy, color, outline, 45.0);
        }
        CrosshairStyle::Circle => {
            push_ring(&mut out, d, cx, cy, color, outline);
        }
        CrosshairStyle::Dot => {}
        CrosshairStyle::Square => {
            push_square(&mut out, d, cx, cy, color, outline);
        }
        CrosshairStyle::CircleCross => {
            push_ring(&mut out, d, cx, cy, color, outline);
            push_cross(&mut out, d, cx, cy, color, outline, 0.0);
        }
        CrosshairStyle::Custom => {
            for shape in &d.custom_shapes {
                push_custom_shape(&mut out, shape, cx, cy, d.opacity);
            }
        }
    }

    if d.inner_lines_enabled {
        push_inner_lines(&mut out, d, cx, cy, color, outline);
    }

    if d.show_center_dot && d.center_dot_radius > 0.0 {
        if d.outline_enabled && d.outline_thickness > 0.0 {
            out.push(DrawCmd::Circle(CircleCmd {
                x: cx,
                y: cy,
                radius: d.center_dot_radius + d.outline_thickness,
                color: outline,
            }));
        }
        out.push(DrawCmd::Circle(CircleCmd {
            x: cx,
            y: cy,
            radius: d.center_dot_radius,
            color,
        }));
    }

    let total_rotation = d.rotation;
    if total_rotation.abs() > 0.001 {
        rotate_cmds(&mut out, cx, cy, total_rotation);
    }

    out
}

fn push_cross(
    out: &mut Vec<DrawCmd>,
    d: &crate::CrosshairData,
    cx: f32,
    cy: f32,
    color: Rgba8,
    outline: Rgba8,
    extra_rotation_deg: f32,
) {
    let mut arms: Vec<DrawCmd> = Vec::new();

    if d.show_top {
        push_segment(
            &mut arms,
            cx,
            cy - d.gap,
            cx,
            cy - d.gap - d.height,
            d,
            color,
            outline,
        );
    }
    if d.show_bottom {
        push_segment(
            &mut arms,
            cx,
            cy + d.gap,
            cx,
            cy + d.gap + d.height,
            d,
            color,
            outline,
        );
    }
    if d.show_left {
        push_segment(
            &mut arms,
            cx - d.gap,
            cy,
            cx - d.gap - d.width,
            cy,
            d,
            color,
            outline,
        );
    }
    if d.show_right {
        push_segment(
            &mut arms,
            cx + d.gap,
            cy,
            cx + d.gap + d.width,
            cy,
            d,
            color,
            outline,
        );
    }

    if extra_rotation_deg.abs() > 0.001 {
        rotate_cmds(&mut arms, cx, cy, extra_rotation_deg);
    }

    out.append(&mut arms);
}

fn push_ring(
    out: &mut Vec<DrawCmd>,
    d: &crate::CrosshairData,
    cx: f32,
    cy: f32,
    color: Rgba8,
    outline: Rgba8,
) {
    if d.outline_enabled && d.outline_thickness > 0.0 {
        out.push(DrawCmd::StrokeCircle(StrokeCircleCmd {
            x: cx,
            y: cy,
            radius: d.circle_radius,
            stroke_width: d.circle_thickness + d.outline_thickness * 2.0,
            color: outline,
        }));
    }
    out.push(DrawCmd::StrokeCircle(StrokeCircleCmd {
        x: cx,
        y: cy,
        radius: d.circle_radius,
        stroke_width: d.circle_thickness,
        color,
    }));
}

fn push_square(
    out: &mut Vec<DrawCmd>,
    d: &crate::CrosshairData,
    cx: f32,
    cy: f32,
    color: Rgba8,
    outline: Rgba8,
) {
    let half = d.circle_radius;
    let edges = [
        (cx - half, cy - half, cx + half, cy - half),
        (cx - half, cy + half, cx + half, cy + half),
        (cx - half, cy - half, cx - half, cy + half),
        (cx + half, cy - half, cx + half, cy + half),
    ];
    for (x1, y1, x2, y2) in edges {
        push_segment(out, x1, y1, x2, y2, d, color, outline);
    }
}

fn push_inner_lines(
    out: &mut Vec<DrawCmd>,
    d: &crate::CrosshairData,
    cx: f32,
    cy: f32,
    color: Rgba8,
    outline: Rgba8,
) {
    let start = d.inner_lines_gap;
    let end = d.inner_lines_gap + d.inner_lines_length;

    let fake_d = crate::CrosshairData {
        thickness: d.inner_lines_thickness,
        outline_enabled: d.outline_enabled,
        outline_thickness: d.outline_thickness,
        ..d.clone()
    };

    push_segment(out, cx, cy - start, cx, cy - end, &fake_d, color, outline);
    push_segment(out, cx, cy + start, cx, cy + end, &fake_d, color, outline);
    push_segment(out, cx - start, cy, cx - end, cy, &fake_d, color, outline);
    push_segment(out, cx + start, cy, cx + end, cy, &fake_d, color, outline);
}

fn push_custom_shape(
    out: &mut Vec<DrawCmd>,
    shape: &crate::model::CustomShape,
    cx: f32,
    cy: f32,
    opacity: f32,
) {
    let color = with_opacity(shape.color, opacity);
    let outline = with_opacity(shape.outline_color, opacity);
    let ot = shape.outline_thickness;

    match &shape.kind {
        CustomShapeKind::Line { x1, y1, x2, y2 } => {
            let (ax1, ay1) = (cx + x1, cy + y1);
            let (ax2, ay2) = (cx + x2, cy + y2);
            if shape.outline_enabled && ot > 0.0 {
                out.push(DrawCmd::Line(LineCmd {
                    x1: ax1,
                    y1: ay1,
                    x2: ax2,
                    y2: ay2,
                    thickness: shape.thickness + ot * 2.0,
                    color: outline,
                }));
            }
            out.push(DrawCmd::Line(LineCmd {
                x1: ax1,
                y1: ay1,
                x2: ax2,
                y2: ay2,
                thickness: shape.thickness,
                color,
            }));
        }

        CustomShapeKind::Circle {
            cx: sx,
            cy: sy,
            radius,
            filled: true,
        } => {
            let (scx, scy) = (cx + sx, cy + sy);
            if shape.outline_enabled && ot > 0.0 {
                out.push(DrawCmd::Circle(CircleCmd {
                    x: scx,
                    y: scy,
                    radius: radius + ot,
                    color: outline,
                }));
            }
            out.push(DrawCmd::Circle(CircleCmd {
                x: scx,
                y: scy,
                radius: *radius,
                color,
            }));
        }

        CustomShapeKind::Circle {
            cx: sx,
            cy: sy,
            radius,
            filled: false,
        } => {
            let (scx, scy) = (cx + sx, cy + sy);
            if shape.outline_enabled && ot > 0.0 {
                out.push(DrawCmd::StrokeCircle(StrokeCircleCmd {
                    x: scx,
                    y: scy,
                    radius: *radius,
                    stroke_width: shape.thickness + ot * 2.0,
                    color: outline,
                }));
            }
            out.push(DrawCmd::StrokeCircle(StrokeCircleCmd {
                x: scx,
                y: scy,
                radius: *radius,
                stroke_width: shape.thickness,
                color,
            }));
        }

        CustomShapeKind::Rect {
            x: sx,
            y: sy,
            w,
            h,
            filled: true,
        } => {
            let (rx, ry) = (cx + sx, cy + sy);
            if shape.outline_enabled && ot > 0.0 {
                out.push(DrawCmd::FilledRect(FilledRectCmd {
                    x: rx - ot,
                    y: ry - ot,
                    w: w + ot * 2.0,
                    h: h + ot * 2.0,
                    color: outline,
                }));
            }
            out.push(DrawCmd::FilledRect(FilledRectCmd {
                x: rx,
                y: ry,
                w: *w,
                h: *h,
                color,
            }));
        }

        CustomShapeKind::Rect {
            x: sx,
            y: sy,
            w,
            h,
            filled: false,
        } => {
            let (rx, ry) = (cx + sx, cy + sy);
            if shape.outline_enabled && ot > 0.0 {
                out.push(DrawCmd::StrokeRect(StrokeRectCmd {
                    x: rx,
                    y: ry,
                    w: *w,
                    h: *h,
                    stroke_width: shape.thickness + ot * 2.0,
                    color: outline,
                }));
            }
            out.push(DrawCmd::StrokeRect(StrokeRectCmd {
                x: rx,
                y: ry,
                w: *w,
                h: *h,
                stroke_width: shape.thickness,
                color,
            }));
        }

        CustomShapeKind::Triangle {
            x1: sx1,
            y1: sy1,
            x2: sx2,
            y2: sy2,
            x3: sx3,
            y3: sy3,
            filled,
        } => {
            let (ax1, ay1) = (cx + sx1, cy + sy1);
            let (ax2, ay2) = (cx + sx2, cy + sy2);
            let (ax3, ay3) = (cx + sx3, cy + sy3);

            if *filled {
                if shape.outline_enabled && ot > 0.0 {
                    let ot2 = shape.thickness + ot * 2.0;
                    out.push(DrawCmd::Line(LineCmd {
                        x1: ax1,
                        y1: ay1,
                        x2: ax2,
                        y2: ay2,
                        thickness: ot2,
                        color: outline,
                    }));
                    out.push(DrawCmd::Line(LineCmd {
                        x1: ax2,
                        y1: ay2,
                        x2: ax3,
                        y2: ay3,
                        thickness: ot2,
                        color: outline,
                    }));
                    out.push(DrawCmd::Line(LineCmd {
                        x1: ax3,
                        y1: ay3,
                        x2: ax1,
                        y2: ay1,
                        thickness: ot2,
                        color: outline,
                    }));
                }
                out.push(DrawCmd::FilledTriangle(FilledTriangleCmd {
                    x1: ax1,
                    y1: ay1,
                    x2: ax2,
                    y2: ay2,
                    x3: ax3,
                    y3: ay3,
                    color,
                }));
            } else {
                let sw = shape.thickness;
                let osw = sw + ot * 2.0;
                let edges = [
                    (ax1, ay1, ax2, ay2),
                    (ax2, ay2, ax3, ay3),
                    (ax3, ay3, ax1, ay1),
                ];
                for (ex1, ey1, ex2, ey2) in edges {
                    if shape.outline_enabled && ot > 0.0 {
                        out.push(DrawCmd::Line(LineCmd {
                            x1: ex1,
                            y1: ey1,
                            x2: ex2,
                            y2: ey2,
                            thickness: osw,
                            color: outline,
                        }));
                    }
                    out.push(DrawCmd::Line(LineCmd {
                        x1: ex1,
                        y1: ey1,
                        x2: ex2,
                        y2: ey2,
                        thickness: sw,
                        color,
                    }));
                }
            }
        }
    }
}

fn push_segment(
    out: &mut Vec<DrawCmd>,
    x1: f32,
    y1: f32,
    x2: f32,
    y2: f32,
    d: &crate::CrosshairData,
    color: Rgba8,
    outline: Rgba8,
) {
    if d.outline_enabled && d.outline_thickness > 0.0 {
        out.push(DrawCmd::Line(LineCmd {
            x1,
            y1,
            x2,
            y2,
            thickness: d.thickness + d.outline_thickness * 2.0,
            color: outline,
        }));
    }
    out.push(DrawCmd::Line(LineCmd {
        x1,
        y1,
        x2,
        y2,
        thickness: d.thickness,
        color,
    }));
}

fn rotate_cmds(cmds: &mut Vec<DrawCmd>, cx: f32, cy: f32, angle_deg: f32) {
    let angle_rad = angle_deg * PI / 180.0;
    let cos = angle_rad.cos();
    let sin = angle_rad.sin();

    for cmd in cmds.iter_mut() {
        match cmd {
            DrawCmd::Line(line) => {
                (line.x1, line.y1) = rotate_pt(line.x1, line.y1, cx, cy, cos, sin);
                (line.x2, line.y2) = rotate_pt(line.x2, line.y2, cx, cy, cos, sin);
            }
            DrawCmd::Circle(c) => {
                (c.x, c.y) = rotate_pt(c.x, c.y, cx, cy, cos, sin);
            }
            DrawCmd::StrokeCircle(c) => {
                (c.x, c.y) = rotate_pt(c.x, c.y, cx, cy, cos, sin);
            }
            DrawCmd::FilledRect(_) | DrawCmd::StrokeRect(_) => {}
            DrawCmd::FilledTriangle(t) => {
                (t.x1, t.y1) = rotate_pt(t.x1, t.y1, cx, cy, cos, sin);
                (t.x2, t.y2) = rotate_pt(t.x2, t.y2, cx, cy, cos, sin);
                (t.x3, t.y3) = rotate_pt(t.x3, t.y3, cx, cy, cos, sin);
            }
        }
    }
}

#[inline]
fn rotate_pt(x: f32, y: f32, cx: f32, cy: f32, cos: f32, sin: f32) -> (f32, f32) {
    let dx = x - cx;
    let dy = y - cy;
    (cx + dx * cos - dy * sin, cy + dx * sin + dy * cos)
}

fn with_opacity(mut color: Rgba8, opacity: f32) -> Rgba8 {
    color.a = ((color.a as f32) * opacity.clamp(0.0, 1.0)).round() as u8;
    color
}
