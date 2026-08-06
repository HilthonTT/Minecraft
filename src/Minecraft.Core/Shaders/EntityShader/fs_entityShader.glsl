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

// Kept the same as the one the blocks are drawn with, so that a mob standing on distant ground goes under
// the haze at the same rate the ground does. See the comment there for why the distance ignores height.
float fogFactorAt(vec3 fragmentWorldPosition)
{
	float distanceFromCamera = length(fragmentWorldPosition.xz - cameraPosition.xz);
	return smoothstep(fogStart, fogEnd, distanceFromCamera);
}

void main()
{
   vec4 albedo = texture(skinTexture, uv);

   // Skins leave the parts of the sheet no face is cut from fully transparent, and a mob's own artwork may be
   // any colour it likes, white wool included. So what is thrown away here is what the sheet left blank.
   if(albedo.a < 0.5)
   {
		discard;
   }

   fragmentColor = vec4(albedo.rgb * illumination, 1.0);
   fragmentColor.rgb = mix(fragmentColor.rgb, fogColor, fogFactorAt(worldPosition));

   normalDepthColor = vec4(normal, 1.0);
}
