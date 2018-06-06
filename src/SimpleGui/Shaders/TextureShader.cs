﻿using SimpleGui;
using Veldrid;

namespace DEngine.Render
{
    internal class TextureShader : ShaderAbstract
    {
        public DeviceBuffer ProjectionBuffer;
        public DeviceBuffer WorldBuffer;
        public ResourceSet ResourceSet;
        public ResourceLayout ProjViewLayout;
        public ResourceLayout TextureLayout;
        
        public TextureShader(ResourceFactory factory) : base(factory, "Tex")
        {
            Layout = new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                        new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                        new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float4));

            ProjectionBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            WorldBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            ProjViewLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("World", ResourceKind.UniformBuffer, ShaderStages.Vertex))
                    );

            TextureLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            ResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                ProjViewLayout,
                ProjectionBuffer,
                WorldBuffer));
        }

        protected override void Dispose(bool disposeManagedResources)
        {
            base.Dispose(disposeManagedResources);

            ResourceSet.Dispose();
            ResourceSet = null;
            TextureLayout.Dispose();
            TextureLayout = null;
            ProjectionBuffer.Dispose();
            ProjectionBuffer = null;
            WorldBuffer.Dispose();
            WorldBuffer = null;
        }
    }
}
