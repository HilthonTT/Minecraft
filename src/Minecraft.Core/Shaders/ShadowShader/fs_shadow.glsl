#version 400 core

in vec2 uv;

uniform sampler2D textureAtlas;

void main()
{
	if (texture(textureAtlas, uv).a < 0.5)
	{
		discard;
	}
}
