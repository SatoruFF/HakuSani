using Godot;

namespace HakuSani;

public static class Shaders
{
    static Shader _ghost, _glow, _water, _footprint, _fade, _vortex, _ripple, _beam;

    public static Shader Ghost => _ghost ??= new Shader
    {
        Code = @"
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_back, fog_disabled;
uniform vec4 tint : source_color = vec4(0.4, 0.6, 1.0, 1.0);
uniform float strength = 1.0;
void fragment() {
    float fres = pow(1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0), 2.0);
    float flicker = 0.85 + 0.15 * sin(TIME * 7.0 + VERTEX.y * 40.0);
    ALBEDO = tint.rgb * (0.25 + fres * 1.6) * flicker;
    ALPHA = strength;
    ALBEDO *= strength;
}",
    };

    /// <summary>Pulsing ring on the floor that marks a time-linked spot.</summary>
    public static Shader Glow => _glow ??= new Shader
    {
        Code = @"
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled, fog_disabled;
uniform vec4 color : source_color = vec4(1.0, 0.85, 0.5, 1.0);
uniform float intensity = 1.0;
void fragment() {
    vec2 p = UV * 2.0 - 1.0;
    float r = length(p);
    float ring = smoothstep(0.08, 0.0, abs(r - 0.75 - 0.05 * sin(TIME * 2.0)));
    float core = smoothstep(1.0, 0.0, r) * 0.35;
    float swirl = 0.5 + 0.5 * sin(atan(p.y, p.x) * 5.0 + TIME * 2.5 - r * 8.0);
    float a = (ring + core * swirl) * smoothstep(1.0, 0.85, r);
    ALBEDO = color.rgb * a * intensity * 1.6;
    ALPHA = 1.0;
}",
    };

    /// <summary>Time vortex backdrop: warm gold past on the left, cold blue future on the right.</summary>
    public static Shader Vortex => _vortex ??= new Shader
    {
        Code = @"
shader_type spatial;
render_mode unshaded, cull_disabled, fog_disabled, shadows_disabled;
uniform float intensity = 1.0;
uniform float split = 0.0;
void fragment() {
    vec2 p = UV * 2.0 - 1.0;
    float r = length(p);
    float a = atan(p.y, p.x);
    float swirl = 0.55 + 0.45 * sin(a * 7.0 - log(r + 0.002) * 9.0 + TIME * 2.2);
    float rings = 0.6 + 0.4 * sin(r * 28.0 - TIME * 5.0);
    float streaks = pow(0.5 + 0.5 * sin(a * 23.0 + TIME * 1.3), 12.0) * 0.5;
    vec3 gold = vec3(1.0, 0.68, 0.25);
    vec3 blue = vec3(0.25, 0.55, 1.0);
    float side = smoothstep(-0.12 - split, 0.12 + split, p.x + 0.12 * sin(p.y * 5.0 + TIME * 1.5));
    vec3 col = mix(gold, blue, side);
    float glow = (0.35 + 0.65 * swirl) * rings * smoothstep(1.05, 0.1, r) + streaks * smoothstep(1.0, 0.3, r);
    glow *= smoothstep(0.03, 0.22, r);
    float seam = smoothstep(0.06, 0.0, abs(p.x + 0.12 * sin(p.y * 5.0 + TIME * 1.5))) * smoothstep(0.9, 0.2, r) * split * 3.0;
    ALBEDO = col * glow * intensity * 0.75 + vec3(1.0) * seam * 0.7;
}",
    };

    /// <summary>Expanding ring of light: "something changed here across time". t goes 0 → 1.</summary>
    public static Shader Ripple => _ripple ??= new Shader
    {
        Code = @"
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled, fog_disabled;
uniform vec4 color : source_color = vec4(0.7, 0.85, 1.0, 1.0);
uniform float t = 0.0;
void fragment() {
    vec2 p = UV * 2.0 - 1.0;
    float r = length(p);
    float front = t;
    float ring = smoothstep(0.12, 0.0, abs(r - front)) * (1.0 - t);
    float inner = smoothstep(front, 0.0, r) * 0.25 * (1.0 - t);
    float a = (ring + inner) * smoothstep(1.0, 0.9, r);
    ALBEDO = color.rgb * a * 2.5;
    ALPHA = 1.0;
}",
    };

    /// <summary>Soft vertical light column (exit portals).</summary>
    public static Shader Beam => _beam ??= new Shader
    {
        Code = @"
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled, fog_disabled;
uniform vec4 color : source_color = vec4(1.0, 0.95, 0.8, 1.0);
void fragment() {
    float edge = pow(1.0 - abs(dot(NORMAL, VIEW)), 1.5);
    float up = UV.y;
    float fade = smoothstep(0.0, 0.08, up) * (1.0 - up);
    float flow = 0.7 + 0.3 * sin(up * 30.0 + TIME * 3.0);
    ALBEDO = color.rgb * (0.15 + (1.0 - edge) * 0.35) * fade * flow;
    ALPHA = 1.0;
}",
    };

    public static Shader Water => _water ??= new Shader
    {
        Code = @"
shader_type spatial;
render_mode cull_disabled, specular_schlick_ggx;
uniform vec4 deep : source_color = vec4(0.10, 0.30, 0.38, 1.0);
uniform vec4 shallow : source_color = vec4(0.35, 0.65, 0.65, 1.0);
void vertex() {
    VERTEX.y += sin(VERTEX.x * 2.0 + TIME * 1.5) * 0.02 + cos(VERTEX.z * 1.7 + TIME) * 0.02;
}
void fragment() {
    vec2 uv = UV * 8.0;
    float w = sin(uv.x * 3.0 + TIME * 1.3) * cos(uv.y * 2.0 - TIME * 0.9);
    ALBEDO = mix(deep.rgb, shallow.rgb, 0.5 + 0.25 * w);
    ROUGHNESS = 0.08;
    METALLIC = 0.0;
    SPECULAR = 0.7;
    ALPHA = 0.88;
}",
    };

    public static Shader Footprint => _footprint ??= new Shader
    {
        Code = @"
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled, fog_disabled;
uniform sampler2D tex : source_color;
uniform vec4 color : source_color = vec4(0.4, 0.7, 1.0, 1.0);
uniform float life = 1.0;
void fragment() {
    float a = texture(tex, UV).a;
    float pulse = 0.8 + 0.2 * sin(TIME * 3.0);
    ALBEDO = color.rgb * a * life * pulse * 1.8;
    ALPHA = 1.0;
}",
    };
}
