shader_type spatial;
render_mode unshaded;

uniform vec4 color : source_color;

uniform float opacity = 1.0f;

void fragment() {
	ALBEDO = color.rgb;
	ALPHA = clamp(1.0 - UV.x, 0.0, 1.0) * opacity;
}
