#version 400 core
layout (location = 0) in vec3 vertexPosition;
layout (location = 1) in vec3 vertexNormal;
layout (location = 2) in vec2 vertexUv;
layout (location = 3) in uint vertexIllumination;

out vec2 uv;
out float brightness;
out float sunlight;
out vec3 rgbColor;
out vec3 worldPosition;
out vec3 normal;
flat out uint material;

uniform mat4 transformationMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;
uniform float animationTime;

vec3 windOffset(vec3 position, uint kind)
{
	float strength = kind == 2u ? 0.07 : 0.028;
	float gust = 0.65 + 0.35 * sin(animationTime * 0.37 + position.x * 0.043 + position.z * 0.031);
	float phase = animationTime * 1.8 + position.x * 0.57 + position.z * 0.41 + position.y * 0.23;
	vec3 sway = vec3(sin(phase), 0.3 * sin(phase * 1.31 + 1.7), cos(phase * 0.83 + position.y * 0.7));
	return sway * strength * gust;
}

void main()
{
	vec4 worldSpacePosition = transformationMatrix * vec4(vertexPosition, 1.0);

	material = (vertexIllumination >> 30) & 3u;
	if (material == 1u || material == 2u)
	{
		worldSpacePosition.xyz += windOffset(worldSpacePosition.xyz, material);
	}

	gl_Position = projectionMatrix * viewMatrix * worldSpacePosition;
	uv = vertexUv;
	worldPosition = worldSpacePosition.xyz;
	normal = vertexNormal;

	brightness = float((vertexIllumination >> 24) & 0x3Fu) / 64.0;
	sunlight = float((vertexIllumination >> 18) & 0x3Fu) / 64.0;
	float red = float(vertexIllumination & 0x3Fu) / 64.0;
	float green = float((vertexIllumination >> 6) & 0x3Fu) / 64.0;
	float blue = float((vertexIllumination >> 12) & 0x3Fu) / 64.0;
	rgbColor = vec3(red, green, blue);
}
