#version 400 core
out vec4 outputColor;

in vec2 uv;

uniform sampler2D sourceTexture;
uniform vec2 sourceTexelSize;
uniform float threshold;

void main()
{
	vec2 offset = sourceTexelSize;
	vec3 color = texture(sourceTexture, uv + vec2(-offset.x, -offset.y)).rgb;
	color += texture(sourceTexture, uv + vec2(offset.x, -offset.y)).rgb;
	color += texture(sourceTexture, uv + vec2(-offset.x, offset.y)).rgb;
	color += texture(sourceTexture, uv + vec2(offset.x, offset.y)).rgb;
	color *= 0.25;

	float peak = max(color.r, max(color.g, color.b));
	float knee = threshold * 0.5;
	float soft = clamp(peak - threshold + knee, 0.0, 2.0 * knee);
	soft = soft * soft / (4.0 * knee + 0.0001);
	float contribution = max(soft, peak - threshold) / max(peak, 0.0001);

	outputColor = vec4(color * contribution, 1.0);
}
