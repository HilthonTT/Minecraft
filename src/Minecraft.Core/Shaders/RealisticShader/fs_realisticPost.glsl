#version 400 core
out vec4 outputColor;

in vec2 fragmentPosition;
in vec2 uv;

uniform sampler2D colorTexture;
uniform sampler2D bloomTexture;
uniform float exposure;
uniform float bloomStrength;

const vec3 lumaWeights = vec3(0.2126, 0.7152, 0.0722);

const float shoulderStart = 0.72;

vec3 filmicShoulder(vec3 color)
{
	vec3 over = max(color - shoulderStart, 0.0);
	vec3 compressed = shoulderStart + (1.0 - shoulderStart) * (1.0 - exp(-over / (1.0 - shoulderStart)));
	return mix(color, compressed, step(shoulderStart, color));
}

vec3 grade(vec3 color)
{
	float luma = dot(color, lumaWeights);
	color = mix(vec3(luma), color, 1.06);
	color = clamp(color, 0.0, 1.0);
	color = mix(color, color * color * (3.0 - 2.0 * color), 0.12);
	vec3 shadowTint = vec3(0.97, 0.99, 1.04);
	vec3 highlightTint = vec3(1.03, 1.0, 0.96);
	color *= mix(shadowTint, highlightTint, smoothstep(0.1, 0.8, luma));
	return clamp(color, 0.0, 1.0);
}

float vignette()
{
	vec2 relativeToCenter = uv - 0.5;
	float distanceToCenter = length(relativeToCenter);
	return mix(1.0, smoothstep(0.85, 0.35, distanceToCenter), 0.3);
}

void main()
{
	vec3 scene = max(texture(colorTexture, uv).rgb, 0.0);
	vec3 bloom = texture(bloomTexture, uv).rgb;
	vec3 hdr = scene + bloom * bloomStrength;

	vec3 mapped = filmicShoulder(hdr * exposure);

	outputColor = vec4(grade(mapped) * vignette(), 1.0);
}
