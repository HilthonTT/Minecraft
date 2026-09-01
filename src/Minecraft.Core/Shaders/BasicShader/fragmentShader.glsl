#version 400 core
layout (location = 0) out vec4 fragmentColor;
layout (location = 1) out vec4 normalDepthColor;

in vec2 uv;
in float brightness;
in float sunlight;
in vec3 rgbColor;
in vec3 position;
in vec3 worldPosition;
in vec3 normal;

uniform sampler2D textureAtlas;
uniform vec3 sunColor;
uniform vec3 ambientColor;

uniform vec3 cameraPosition;
uniform vec3 fogColor;
uniform float fogStart;
uniform float fogEnd;

uniform float materialAlpha;

float convertRange(float oldMin, float oldMax, float newMin, float newMax, float oldValue)
{
    float oldRange = oldMax - oldMin;
    float newRange = newMax - newMin;
    return (((oldValue - oldMin) * newRange) / oldRange) + newMin;
}

float fogFactorAt(vec3 fragmentWorldPosition)
{
	float distanceFromCamera = length(fragmentWorldPosition.xz - cameraPosition.xz);

	return smoothstep(fogStart, fogEnd, distanceFromCamera);
}

void main()
{
   vec4 albedo = texture(textureAtlas, uv);

   if(albedo.a < 0.5F)
   {
		discard;
   }

   vec4 materialColor = albedo * vec4(rgbColor, 1.0F) + albedo * vec4(sunColor, 1.0F) * sunlight;
   materialColor.x = convertRange(0, 1, 0, 1 - ambientColor.x, materialColor.x);
   materialColor.y = convertRange(0, 1, 0, 1 - ambientColor.y, materialColor.y);
   materialColor.z = convertRange(0, 1, 0, 1 - ambientColor.z, materialColor.z);

   fragmentColor = (materialColor + albedo * vec4(ambientColor, 1.0F)) * brightness;

   fragmentColor.rgb = mix(fragmentColor.rgb, fogColor, fogFactorAt(worldPosition));

   fragmentColor.a = materialAlpha;

   normalDepthColor = vec4(normal, 1.0F);
}
