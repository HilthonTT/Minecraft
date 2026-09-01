#version 400 core
layout (location = 0) out vec4 fragmentColor;
layout (location = 1) out vec4 normalDepthColor;

in vec2 uv;
in float illumination;
in vec3 position;
in vec3 worldPosition;
in vec3 normal;

uniform sampler2D skinTexture;

uniform vec3 cameraPosition;
uniform vec3 fogColor;
uniform float fogStart;
uniform float fogEnd;

uniform float hurtFlash;

const vec3 hurtColor = vec3(1.0, 0.0, 0.0);

float fogFactorAt(vec3 fragmentWorldPosition)
{
	float distanceFromCamera = length(fragmentWorldPosition.xz - cameraPosition.xz);
	return smoothstep(fogStart, fogEnd, distanceFromCamera);
}

void main()
{
   vec4 albedo = texture(skinTexture, uv);

   if(albedo.a < 0.5)
   {
		discard;
   }

   fragmentColor = vec4(albedo.rgb * illumination, 1.0);

   fragmentColor.rgb = mix(fragmentColor.rgb, hurtColor, hurtFlash);
   fragmentColor.rgb = mix(fragmentColor.rgb, fogColor, fogFactorAt(worldPosition));

   normalDepthColor = vec4(normal, 1.0);
}
