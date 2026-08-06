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

float convertRange(float oldMin, float oldMax, float newMin, float newMax, float oldValue)
{
    float oldRange = oldMax - oldMin;
    float newRange = newMax - newMin;
    return (((oldValue - oldMin) * newRange) / oldRange) + newMin;
}

// How much of the fog colour a fragment has taken on, from none of it to all of it.
//
// The distance is measured flat, ignoring height, because that is the shape the world is loaded in: chunks
// are columns the full height of the world, kept or dropped on how far away they are horizontally. Fog that
// went by true distance would close over the ground far below somebody flying high up, where the world is
// still loaded and there is nothing to hide, and would leave the edge of a chunk at eye level showing.
float fogFactorAt(vec3 fragmentWorldPosition)
{
	float distanceFromCamera = length(fragmentWorldPosition.xz - cameraPosition.xz);

	// Smoothstep rather than a linear ramp, so that the fog eases in instead of putting a visible crease
	// across the ground along the circle where it starts.
	return smoothstep(fogStart, fogEnd, distanceFromCamera);
}

void main()
{
   vec4 albedo = texture(textureAtlas, uv);

   // The see through parts of the cut out cells are cleared to a zero alpha when the sheet is loaded. Testing
   // the alpha rather than the colour is what lets snow and ice stay the near white they are drawn.
   if(albedo.a < 0.5F)
   {
		discard;
   }

   vec4 materialColor = albedo * vec4(rgbColor, 1.0F) + albedo * vec4(sunColor, 1.0F) * sunlight;
   materialColor.x = convertRange(0, 1, 0, 1 - ambientColor.x, materialColor.x);
   materialColor.y = convertRange(0, 1, 0, 1 - ambientColor.y, materialColor.y);
   materialColor.z = convertRange(0, 1, 0, 1 - ambientColor.z, materialColor.z);

   fragmentColor = (materialColor + albedo * vec4(ambientColor, 1.0F)) * brightness;

   // Applied last, over the lit colour, since fog is what sits between the block and the eye rather than
   // anything about the block itself. A fragment out at the far edge is left as pure fog colour, which is
   // what hides the edge of the loaded world.
   fragmentColor.rgb = mix(fragmentColor.rgb, fogColor, fogFactorAt(worldPosition));

   normalDepthColor = vec4(normal, 1.0F);
}