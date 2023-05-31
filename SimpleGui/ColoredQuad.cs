﻿using System.Drawing;
using System.Numerics;
using SimpleGui.Scene;
using Veldrid;

namespace SimpleGui
{
    public class ColoredQuad : SceneNode
    {
        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;

        public Color Color { get; set; }

        public virtual void Initialize()
        {
            Vector2 topLeft = new Vector2(0, 0);
            Vector2 topRight = new Vector2(0 + Size.X, 0);
            Vector2 bottomLeft = new Vector2(0, 0 + Size.Y);
            Vector2 bottomRight = new Vector2(0 + Size.X, 0 + Size.Y);
            
            VertexPositionColor[] quadVertices =
            {
                new VertexPositionColor(topLeft, Color.ToVector4()),
                new VertexPositionColor(topRight, Color.ToVector4()),
                new VertexPositionColor(bottomLeft, Color.ToVector4()),
                new VertexPositionColor(bottomRight, Color.ToVector4())
            };

            var vbDescription = new BufferDescription(
                32 * VertexPositionColor.SizeInBytes,
                BufferUsage.VertexBuffer);
            _vertexBuffer = Gui.Factory.CreateBuffer(vbDescription);
            Gui.Device.UpdateBuffer(_vertexBuffer, 0, quadVertices);

            ushort[] quadIndices = { 0, 1, 2, 3 };
            BufferDescription ibDescription = new BufferDescription(
                32 * sizeof(ushort),
                BufferUsage.IndexBuffer);
            _indexBuffer = Gui.Factory.CreateBuffer(ibDescription);
            Gui.Device.UpdateBuffer(_indexBuffer, 0, quadIndices);
        }

        protected override void Dispose(bool disposeManagedResources)
        {
            base.Dispose(disposeManagedResources);

            if (_vertexBuffer != null && _indexBuffer != null)
            {
                _vertexBuffer.Dispose();
                _vertexBuffer = null;
                _indexBuffer.Dispose();
                _indexBuffer = null;
            }
        }

        public void Recreate()
        {
            if (_vertexBuffer != null)
            {
                _vertexBuffer.Dispose();
                _indexBuffer.Dispose();
            }

            Initialize();
        }


        public override void Draw()
        {
            Gui.BeginDraw();
            DrawBatched();
            Gui.EndDraw();

            base.Draw();
        }

        public void DrawBatched()
        {
            Gui.CommandList.UpdateBuffer(Gui.ColorShader.WorldBuffer, 0, Matrix4x4.CreateTranslation(new Vector3(AbsolutePosition, 0)));
            Gui.CommandList.SetVertexBuffer(0, _vertexBuffer);
            Gui.CommandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            //Gui.CommandList.SetGraphicsResourceSet(0, Gui.ColorShader.ResourceSet);
            // Issue a Draw command for a single instance with 4 indices.
            Gui.CommandList.DrawIndexed(
                indexCount: 4,
                instanceCount: 1,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);

        }
    }
}
