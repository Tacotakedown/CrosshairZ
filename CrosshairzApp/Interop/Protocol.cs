using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace CrosshairZ.Interop
{
    public enum CrosshairStyle
    {
        [JsonProperty("classic")] Classic = 0,
        [JsonProperty("circle")] Circle = 1,
        [JsonProperty("dot")] Dot = 2,
        [JsonProperty("diagonal_x")] DiagonalX = 3,
        [JsonProperty("square")] Square = 4,
        [JsonProperty("circle_cross")] CircleCross = 5,
    }

    public sealed class Rgba8
    {
        [JsonProperty("r")] public byte R { get; set; }
        [JsonProperty("g")] public byte G { get; set; }
        [JsonProperty("b")] public byte B { get; set; }
        [JsonProperty("a")] public byte A { get; set; } = 255;
    }

    public sealed class CrosshairData
    {
        [JsonProperty("enabled")] public bool Enabled { get; set; } = true;
        [JsonProperty("style")] public string Style { get; set; } = "classic";

        // Arms
        [JsonProperty("show_top")] public bool ShowTop { get; set; } = true;
        [JsonProperty("show_bottom")] public bool ShowBottom { get; set; } = true;
        [JsonProperty("show_left")] public bool ShowLeft { get; set; } = true;
        [JsonProperty("show_right")] public bool ShowRight { get; set; } = true;
        [JsonProperty("show_center_dot")] public bool ShowCenterDot { get; set; } = true;

        [JsonProperty("width")] public float Width { get; set; } = 18f;
        [JsonProperty("height")] public float Height { get; set; } = 18f;
        [JsonProperty("thickness")] public float Thickness { get; set; } = 2f;
        [JsonProperty("gap")] public float Gap { get; set; } = 6f;
        [JsonProperty("center_dot_radius")] public float CenterDotRadius { get; set; } = 1.5f;

        // Appearance
        [JsonProperty("opacity")] public float Opacity { get; set; } = 1f;
        [JsonProperty("color")] public Rgba8 Color { get; set; } = new Rgba8 { R = 255, G = 255, B = 255, A = 255 };
        [JsonProperty("outline_enabled")] public bool OutlineEnabled { get; set; } = true;
        [JsonProperty("outline_thickness")] public float OutlineThickness { get; set; } = 1f;
        [JsonProperty("outline_color")] public Rgba8 OutlineColor { get; set; } = new Rgba8 { R = 0, G = 0, B = 0, A = 255 };

        // Rotation
        [JsonProperty("rotation")] public float Rotation { get; set; } = 0f;
        [JsonProperty("rotation_speed")] public float RotationSpeed { get; set; } = 0f;

        // Inner lines
        [JsonProperty("inner_lines_enabled")] public bool InnerLinesEnabled { get; set; } = false;
        [JsonProperty("inner_lines_length")] public float InnerLinesLength { get; set; } = 4f;
        [JsonProperty("inner_lines_thickness")] public float InnerLinesThickness { get; set; } = 2f;
        [JsonProperty("inner_lines_gap")] public float InnerLinesGap { get; set; } = 3f;

        // Circle / Square style
        [JsonProperty("circle_radius")] public float CircleRadius { get; set; } = 24f;
        [JsonProperty("circle_thickness")] public float CircleThickness { get; set; } = 2f;

        // Custom editor shapes
        [JsonProperty("custom_shapes")] public List<CustomShapeData> CustomShapes { get; set; } = new List<CustomShapeData>();
    }

    public sealed class CustomShapeData
    {
        [JsonProperty("kind")] public CustomShapeKind Kind { get; set; }
        [JsonProperty("color")] public Rgba8 Color { get; set; } = new Rgba8 { R = 255, G = 255, B = 255, A = 255 };
        [JsonProperty("thickness")] public float Thickness { get; set; } = 2f;
        [JsonProperty("outline_enabled")] public bool OutlineEnabled { get; set; } = true;
        [JsonProperty("outline_thickness")] public float OutlineThickness { get; set; } = 1f;
        [JsonProperty("outline_color")] public Rgba8 OutlineColor { get; set; } = new Rgba8 { R = 0, G = 0, B = 0, A = 255 };
    }

    public sealed class CustomShapeKind
    {
        // Discriminant — one of: "line", "circle", "rect", "triangle"
        [JsonProperty("type")] public string Type { get; set; }

        // Line / Triangle shared fields
        [JsonProperty("x1")] public float? X1 { get; set; }
        [JsonProperty("y1")] public float? Y1 { get; set; }
        [JsonProperty("x2")] public float? X2 { get; set; }
        [JsonProperty("y2")] public float? Y2 { get; set; }

        // Triangle third point
        [JsonProperty("x3")] public float? X3 { get; set; }
        [JsonProperty("y3")] public float? Y3 { get; set; }

        // Circle fields
        [JsonProperty("cx")] public float? Cx { get; set; }
        [JsonProperty("cy")] public float? Cy { get; set; }
        [JsonProperty("radius")] public float? Radius { get; set; }
        [JsonProperty("filled")] public bool? Filled { get; set; }

        // Rect fields
        [JsonProperty("x")] public float? X { get; set; }
        [JsonProperty("y")] public float? Y { get; set; }
        [JsonProperty("w")] public float? W { get; set; }
        [JsonProperty("h")] public float? H { get; set; }
    }

    public sealed class CrosshairProfile
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("crosshair")] public CrosshairData Crosshair { get; set; }
    }

    public sealed class ProfilesFile
    {
        [JsonProperty("version")] public int Version { get; set; } = 2;
        [JsonProperty("profiles")] public List<CrosshairProfile> Profiles { get; set; } = new List<CrosshairProfile>();
        [JsonProperty("active_profile_id")] public string ActiveProfileId { get; set; }
    }

    public sealed class BuildPreviewPayload
    {
        [JsonProperty("crosshair")] public CrosshairData Crosshair { get; set; }
        [JsonProperty("width")] public float Width { get; set; }
        [JsonProperty("height")] public float Height { get; set; }
    }

    public sealed class LineCmd
    {
        [JsonProperty("x1")] public float X1 { get; set; }
        [JsonProperty("y1")] public float Y1 { get; set; }
        [JsonProperty("x2")] public float X2 { get; set; }
        [JsonProperty("y2")] public float Y2 { get; set; }
        [JsonProperty("thickness")] public float Thickness { get; set; }
        [JsonProperty("color")] public Rgba8 Color { get; set; }
    }

    public sealed class CircleCmd
    {
        [JsonProperty("x")] public float X { get; set; }
        [JsonProperty("y")] public float Y { get; set; }
        [JsonProperty("radius")] public float Radius { get; set; }
        [JsonProperty("color")] public Rgba8 Color { get; set; }
    }

    public sealed class StrokeCircleCmd
    {
        [JsonProperty("x")] public float X { get; set; }
        [JsonProperty("y")] public float Y { get; set; }
        [JsonProperty("radius")] public float Radius { get; set; }
        [JsonProperty("stroke_width")] public float StrokeWidth { get; set; }
        [JsonProperty("color")] public Rgba8 Color { get; set; }
    }

    public sealed class FilledRectCmd
    {
        [JsonProperty("x")] public float X { get; set; }
        [JsonProperty("y")] public float Y { get; set; }
        [JsonProperty("w")] public float W { get; set; }
        [JsonProperty("h")] public float H { get; set; }
        [JsonProperty("color")] public Rgba8 Color { get; set; }
    }

    public sealed class StrokeRectCmd
    {
        [JsonProperty("x")] public float X { get; set; }
        [JsonProperty("y")] public float Y { get; set; }
        [JsonProperty("w")] public float W { get; set; }
        [JsonProperty("h")] public float H { get; set; }
        [JsonProperty("stroke_width")] public float StrokeWidth { get; set; }
        [JsonProperty("color")] public Rgba8 Color { get; set; }
    }

    public sealed class FilledTriangleCmd
    {
        [JsonProperty("x1")] public float X1 { get; set; }
        [JsonProperty("y1")] public float Y1 { get; set; }
        [JsonProperty("x2")] public float X2 { get; set; }
        [JsonProperty("y2")] public float Y2 { get; set; }
        [JsonProperty("x3")] public float X3 { get; set; }
        [JsonProperty("y3")] public float Y3 { get; set; }
        [JsonProperty("color")] public Rgba8 Color { get; set; }
    }

    public sealed class DrawCmd
    {
        [JsonProperty("Line")] public LineCmd Line { get; set; }
        [JsonProperty("Circle")] public CircleCmd Circle { get; set; }
        [JsonProperty("StrokeCircle")] public StrokeCircleCmd StrokeCircle { get; set; }
        [JsonProperty("FilledRect")] public FilledRectCmd FilledRect { get; set; }
        [JsonProperty("StrokeRect")] public StrokeRectCmd StrokeRect { get; set; }
        [JsonProperty("FilledTriangle")] public FilledTriangleCmd FilledTriangle { get; set; }
    }

    public static class Requests
    {
        public static object GetActiveProfile() => "GetActiveProfile";

        public static object SetActiveCrosshair(CrosshairData data) => new
        {
            SetActiveCrosshair = data
        };

        public static object EncodeShareCode(CrosshairData data) => new
        {
            EncodeShareCode = data
        };

        public static object DecodeShareCode(string code) => new
        {
            DecodeShareCode = code
        };

        public static object BuildPreview(CrosshairData data, float width, float height) => new
        {
            BuildPreview = new BuildPreviewPayload
            {
                Crosshair = data,
                Width = width,
                Height = height
            }
        };
    }

    public sealed class ResponseEnvelope
    {
        [JsonExtensionData]
        public IDictionary<string, JToken> Values { get; set; }
    }

}
