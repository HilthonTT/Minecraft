#version 400 core
layout (location = 0) in vec3 vertexPosition;
layout (location = 1) in vec2 vertexUv;

out vec2 uv;				
out vec2 fragmentPosition;				

void main()
{
	uv = vertexUv;
	fragmentPosition = vec2(vertexPosition) * 0.5 + vec2(0.5, 0.5);
	gl_Position = vec4(vertexPosition, 1);
}