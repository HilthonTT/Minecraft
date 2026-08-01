#version 400 core
layout (location = 0) in vec3 vertexPosition;

uniform mat4 transformationMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

void main()
{
    gl_Position = projectionMatrix * viewMatrix * transformationMatrix  * vec4(vertexPosition, 1.0);
}
