#version 400 core
layout (location = 0) out vec4 fragmentColor;

in vec3 position;

uniform sampler2D ditherTexture;
uniform vec3 sunPosition;
uniform float animationTime;
uniform float daylight;

uniform vec3 topSkyColor;
uniform vec3 bottomSkyColor;
uniform vec3 horizonColor;
uniform vec3 sunColor;
uniform vec3 sunGlowColor;
uniform vec3 moonColor;
uniform vec3 moonGlowColor;

float hash(vec3 cell)
{
	return fract(sin(dot(cell, vec3(12.9898, 78.233, 37.719))) * 43758.5453);
}

vec3 scatteringToward(vec3 direction)
{
	float towardsSun = max(dot(direction, sunPosition), 0.0);
	return sunGlowColor * (pow(towardsSun, 8.0) * 0.35 + pow(towardsSun, 64.0) * 0.6);
}

void main()
{
	vec3 direction = normalize(position);
	float height = direction.y;

	vec3 sky;
	if (height >= 0.0)
	{
		sky = mix(horizonColor, topSkyColor, pow(height, 0.5));
	}
	else
	{
		sky = mix(horizonColor, bottomSkyColor, pow(min(-height * 4.0, 1.0), 0.6));
	}

	sky += scatteringToward(direction);

	float sunCosine = dot(direction, sunPosition);
	float sunDisc = smoothstep(0.99865, 0.99895, sunCosine);
	float aboveHorizon = smoothstep(-0.04, 0.02, height);
	sky += sunColor * sunDisc * 9.0 * aboveHorizon;
	sky += sunGlowColor * pow(max(sunCosine, 0.0), 900.0) * 2.5 * aboveHorizon;

	float moonCosine = dot(direction, -sunPosition);
	float moonDisc = smoothstep(0.99905, 0.99925, moonCosine);
	sky = mix(sky, moonColor * 1.4, moonDisc * aboveHorizon);
	sky += moonGlowColor * pow(max(moonCosine, 0.0), 120.0) * 0.35 * (1.0 - daylight);

	float night = 1.0 - daylight;
	if (night > 0.0 && height > 0.0)
	{
		vec3 cell = floor(direction * 260.0);
		float seed = hash(cell);
		float star = step(0.998, seed);
		float twinkle = 0.65 + 0.35 * sin(animationTime * (1.5 + seed * 3.0) + seed * 80.0);
		float fade = smoothstep(0.0, 0.25, height) * night * (1.0 - moonDisc);
		sky += vec3(0.9, 0.93, 1.0) * star * twinkle * fade * 0.9;
	}

	fragmentColor = vec4(sky, 1.0);

	fragmentColor.rgb += vec3(texture(ditherTexture, gl_FragCoord.xy / 8.0).r / 32.0 - (1.0 / 128.0));
}
