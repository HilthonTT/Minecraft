#version 400 core
out vec4 outputColor;

in vec2 uv;

uniform sampler2D sourceTexture;
uniform vec2 blurStep;

void main()
{
	vec3 color = texture(sourceTexture, uv).rgb * 0.2270270270;
	color += texture(sourceTexture, uv + blurStep * 1.3846153846).rgb * 0.3162162162;
	color += texture(sourceTexture, uv - blurStep * 1.3846153846).rgb * 0.3162162162;
	color += texture(sourceTexture, uv + blurStep * 3.2307692308).rgb * 0.0702702703;
	color += texture(sourceTexture, uv - blurStep * 3.2307692308).rgb * 0.0702702703;
	outputColor = vec4(color, 1.0);
}
