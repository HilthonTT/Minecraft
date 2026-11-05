#version 400 core
layout (location = 0) out vec4 fragmentColor;
layout (location = 1) out vec4 normalDepthColor;

in vec2 uv;
in float brightness;
in float sunlight;
in vec3 rgbColor;
in vec3 worldPosition;
in vec3 normal;
flat in uint material;

uniform sampler2D textureAtlas;
uniform sampler2DShadow shadowMap;

uniform mat4 lightViewProjection;
uniform vec3 lightDirection;
uniform vec3 lightColor;
uniform vec3 skyAmbientColor;
uniform vec3 ambientColor;
uniform vec3 skyTopColor;
uniform vec3 horizonColor;
uniform vec3 sunGlowColor;
uniform vec3 sunDirection;
uniform float shadowStrength;
uniform float shadowTexelSize;
uniform float animationTime;
uniform float atmosphere;

uniform vec3 cameraPosition;
uniform vec3 fogColor;
uniform float fogStart;
uniform float fogEnd;

uniform float materialAlpha;

const float shadowDepthBias = 0.0004;
const float hazeDensity = 0.0035;
const float hazeAmount = 0.28;

float shadowEdgeFade(vec2 coords)
{
	vec2 edge = min(coords, 1.0 - coords);
	return smoothstep(0.0, 0.06, min(edge.x, edge.y));
}

float sunVisibility(vec3 surfaceNormal, bool foliage)
{
	vec3 offset = foliage ? lightDirection * 0.25 : surfaceNormal * 0.06 + lightDirection * 0.03;
	vec4 lightSpace = lightViewProjection * vec4(worldPosition + offset, 1.0);
	vec3 coords = lightSpace.xyz / lightSpace.w * 0.5 + 0.5;

	if (coords.x <= 0.0 || coords.x >= 1.0 || coords.y <= 0.0 || coords.y >= 1.0 || coords.z >= 1.0)
	{
		return 1.0;
	}

	float reference = coords.z - shadowDepthBias;
	float lit = 0.0;
	for (int x = -1; x <= 1; x++)
	{
		for (int y = -1; y <= 1; y++)
		{
			lit += texture(shadowMap, vec3(coords.xy + vec2(x, y) * shadowTexelSize, reference));
		}
	}
	lit /= 9.0;

	return mix(1.0, lit, shadowEdgeFade(coords.xy));
}

vec3 scatteringToward(vec3 direction)
{
	float towardsSun = max(dot(direction, sunDirection), 0.0);
	return sunGlowColor * (pow(towardsSun, 8.0) * 0.35 + pow(towardsSun, 64.0) * 0.6);
}

vec3 skyToward(vec3 direction)
{
	float height = clamp(direction.y, 0.0, 1.0);
	vec3 sky = mix(horizonColor, skyTopColor, pow(height, 0.5));
	return sky + scatteringToward(direction);
}

vec2 waveSlope(vec2 position, vec2 direction, float frequency, float amplitude, float speed)
{
	float phase = dot(position, direction) * frequency + animationTime * speed;
	return direction * (frequency * amplitude * cos(phase));
}

vec3 waterNormal(vec2 position)
{
	vec2 slope = vec2(0.0);
	slope += waveSlope(position, normalize(vec2(0.8, 0.6)), 1.15, 0.045, 1.3);
	slope += waveSlope(position, normalize(vec2(-0.5, 0.9)), 1.9, 0.025, 1.9);
	slope += waveSlope(position, normalize(vec2(0.3, -1.0)), 3.7, 0.012, 2.6);
	slope += waveSlope(position, normalize(vec2(-0.9, -0.2)), 6.1, 0.006, 3.4);
	return normalize(vec3(-slope.x, 1.0, -slope.y));
}

void main()
{
	vec4 albedo = texture(textureAtlas, uv);

	if (albedo.a < 0.5)
	{
		discard;
	}

	bool foliage = material == 1u || material == 2u;
	bool water = material == 3u;

	vec3 surfaceNormal = normalize(normal);
	vec3 toCamera = cameraPosition - worldPosition;
	float cameraDistance = length(toCamera);
	vec3 viewDirection = toCamera / max(cameraDistance, 0.0001);

	if (water)
	{
		if (surfaceNormal.y > 0.5)
		{
			surfaceNormal = waterNormal(worldPosition.xz);
		}

		if (dot(surfaceNormal, viewDirection) < 0.0)
		{
			surfaceNormal = -surfaceNormal;
		}
	}

	float skyExposure = clamp(sunlight * 1.0667, 0.0, 1.0);

	float diffuse = foliage
		? 0.45 + 0.55 * abs(dot(surfaceNormal, lightDirection))
		: max(dot(surfaceNormal, lightDirection), 0.0);

	float visibility = 0.0;
	if (diffuse > 0.0)
	{
		visibility = clamp(skyExposure * 1.6, 0.0, 1.0);
		if (shadowStrength > 0.0)
		{
			visibility *= mix(1.0, sunVisibility(surfaceNormal, foliage), shadowStrength);
		}
		else
		{
			visibility *= smoothstep(0.45, 0.9, skyExposure);
		}
	}

	vec3 direct = lightColor * diffuse * visibility;
	float skyFacing = 0.85 + 0.15 * surfaceNormal.y;
	vec3 ambient = skyAmbientColor * skyExposure * skyFacing + ambientColor;
	vec3 blockLight = rgbColor * 1.2;

	vec3 color = albedo.rgb * (direct + ambient + blockLight) * brightness;
	float alpha = materialAlpha;

	if (water)
	{
		float facing = max(dot(surfaceNormal, viewDirection), 0.0);
		float fresnel = 0.02 + 0.98 * pow(1.0 - facing, 5.0);
		vec3 reflected = reflect(-viewDirection, surfaceNormal);
		float openSky = smoothstep(0.5, 0.9, skyExposure) * atmosphere;

		vec3 reflection = skyToward(reflected);
		color = mix(color, reflection, fresnel * openSky * 0.85);

		float highlight = pow(max(dot(reflected, lightDirection), 0.0), 240.0);
		color += lightColor * highlight * visibility * 5.0 * atmosphere;

		alpha = clamp(mix(materialAlpha - 0.1, 1.0, fresnel * openSky), 0.0, 1.0);
	}

	float fogFactor = smoothstep(fogStart, fogEnd, length(worldPosition.xz - cameraPosition.xz));
	float haze = (1.0 - exp(-cameraDistance * hazeDensity)) * hazeAmount * atmosphere;
	vec3 fogTint = fogColor + scatteringToward(-viewDirection) * atmosphere;
	color = mix(color, fogTint, max(fogFactor, haze));

	fragmentColor = vec4(color, alpha);
	normalDepthColor = vec4(normal, 1.0);
}
