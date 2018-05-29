#version 330 core

struct SamplerDummy { int _dummyValue; };
struct SamplerComparisonDummy { int _dummyValue; };

struct SimpleGui_Resources_Color_VertexInput
{
    vec2 Position;
    vec4 Color;
};

struct SimpleGui_Resources_Color_FragmentInput
{
    vec4 SystemPosition;
    vec4 Color;
};

layout(std140) uniform Projection
{
    mat4 field_Projection;
};

layout(std140) uniform World
{
    vec4 field_World;
};


SimpleGui_Resources_Color_FragmentInput VS( SimpleGui_Resources_Color_VertexInput input_)
{
    SimpleGui_Resources_Color_FragmentInput output_;
    vec4 worldPosition = field_Projection * vec4(input_.Position, 0, 1) + field_World;
    output_.SystemPosition = worldPosition;
    output_.Color = input_.Color;
    return output_;
}


in vec2 Position;
in vec4 Color;
out vec4 fsin_0;

void main()
{
    SimpleGui_Resources_Color_VertexInput input_;
    input_.Position = Position;
    input_.Color = Color;
    SimpleGui_Resources_Color_FragmentInput output_ = VS(input_);
    fsin_0 = output_.Color;
    gl_Position = output_.SystemPosition;
        gl_Position.z = gl_Position.z * 2.0 - gl_Position.w;
}