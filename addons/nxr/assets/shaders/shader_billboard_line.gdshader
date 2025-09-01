shader_type spatial;
render_mode unshaded; // Or shaded if you want lighting

uniform sampler2D line_texture : source_color; // Optional: for textured lines
uniform vec4 line_color : source_color = vec4(1.0);

void vertex() {
    // Vertex position is already billboarded and scaled in C#.
    // No transformation needed here.
    // The VERTEX input is already the final position.
}

void fragment() {
    vec4 final_color = line_color;
    // If you want to use a texture, uncomment the following line
    // final_color *= texture(line_texture, UV);
    ALBEDO = final_color.rgb;
    ALPHA = final_color.a;
}