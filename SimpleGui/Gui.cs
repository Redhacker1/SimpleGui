using System;
using System.IO;
using System.Numerics;
using DEngine.Render;
using SimpleGui.Scene;
using TextRender;
using Veldrid;

namespace SimpleGui
{
    public class Gui : IDisposable
    {
        public SceneGraph SceneGraph { get; set; } = new SceneGraph();
        public static GraphicsDevice Device { get; protected set; }
        public static ResourceFactory Factory { get; protected set; }
        public static CommandList CommandList;
        public static Pipeline Pipeline;
        public static Pipeline TexturePipeline;

        public static ColorShader ColorShader;
        public static TextureShader TextureShader;

        public static GuiSettings Settings { get; set; }

        public static TextRenderer TextRenderer { get; protected set; }
        
        public Gui(GraphicsDevice device)
        {
            LoadSettings();
            
            Device = device;
            Factory = device.ResourceFactory;

            TextRenderer = new TextRenderer(Device);
            
            ColorShader = new ColorShader(Factory);
            TextureShader = new TextureShader(Factory);
            
            // Create pipeline
            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                new DepthStencilStateDescription(
                    depthTestEnabled: true,
                    depthWriteEnabled: true,
                    comparisonKind: ComparisonKind.LessEqual),
                new RasterizerStateDescription(
                    cullMode: FaceCullMode.Back,
                    fillMode: PolygonFillMode.Solid,
                    frontFace: FrontFace.Clockwise,
                    depthClipEnabled: true,
                    scissorTestEnabled: false),
                PrimitiveTopology.TriangleStrip,
                new ShaderSetDescription(
                    vertexLayouts: new[] { ColorShader.Layout },
                    shaders: new[] { ColorShader.VertexShader, ColorShader.FragmentShader }),
                new[] { ColorShader.ResourceLayout },
                Device.SwapchainFramebuffer.OutputDescription
                );

            Pipeline = Factory.CreateGraphicsPipeline(pipelineDescription);


            GraphicsPipelineDescription texturePipelineDesc = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                new DepthStencilStateDescription(
                    depthTestEnabled: true,
                    depthWriteEnabled: true,
                    comparisonKind: ComparisonKind.LessEqual),
                new RasterizerStateDescription(
                    cullMode: FaceCullMode.Back,
                    fillMode: PolygonFillMode.Solid,
                    frontFace: FrontFace.Clockwise,
                    depthClipEnabled: true,
                    scissorTestEnabled: false),
                PrimitiveTopology.TriangleStrip,
                new ShaderSetDescription(
                    vertexLayouts: new[] { TextureShader.Layout },
                    shaders: new[] { TextureShader.VertexShader, TextureShader.FragmentShader }),
                new[] { TextureShader.ProjViewLayout, TextureShader.TextureLayout },
                Device.SwapchainFramebuffer.OutputDescription
                );
            
            TexturePipeline = Factory.CreateGraphicsPipeline(texturePipelineDesc);
            CommandList = Factory.CreateCommandList();
        }
        
        public void Dispose()
        {
            if (SceneGraph != null)
            {
                SceneGraph.DisposeAll();
                SceneGraph = null;
            }

            if (TextRenderer != null)
            {
                TextRenderer.Dispose();
                TextRenderer = null;
            }

            if (CommandList != null)
            {
                CommandList.Dispose();
                CommandList = null;
            }

            if (TexturePipeline != null)
            {
                TexturePipeline.Dispose();
                TexturePipeline = null;
            }

            if (Pipeline != null)
            {
                Pipeline.Dispose();
                Pipeline = null;
            }

            if (TextureShader != null)
            {
                TextureShader.Dispose();
                TextureShader = null;
            }

            if (ColorShader != null)
            {
                ColorShader.Dispose();
                ColorShader = null;
            }

        }


        protected static void LoadSettings()
        {
            const string filename = "gui.yaml";
            if (File.Exists(filename))
            {
                Settings = TinyYaml.FromYamlFile<GuiSettings>(filename);
            }

            Settings ??= new GuiSettings();

            // Debug save
            Settings.ToYamlFile(filename);


            // Copy default theme to default settings
            ControlColorTheme defaultTheme = Settings.Themes[Settings.Theme];
            Settings.DefaultControlSettings.Colors = defaultTheme.Copy();
        }


        public void Update(InputSnapshot snap)
        {
            InputTracker.UpdateFrameInput(snap);

            SceneGraph.Update();
        }

        public static void BeginDraw()
        {
            // Begin() must be called before commands can be issued.
            CommandList.Begin();

            // We want to render directly to the output window.
            CommandList.SetFramebuffer(Device.SwapchainFramebuffer);
            CommandList.SetFullViewports();
            CommandList.UpdateBuffer(ColorShader.ProjectionBuffer, 0, Matrix4x4.CreateOrthographicOffCenter(0, Device.SwapchainFramebuffer.Width, Device.SwapchainFramebuffer.Height, 0, 0, 1));
            CommandList.UpdateBuffer(ColorShader.WorldBuffer, 0, Matrix4x4.CreateTranslation(Vector3.Zero));
            CommandList.SetPipeline(Pipeline);
            CommandList.SetGraphicsResourceSet(0, ColorShader.ResourceSet);
        }

        public static void EndDraw()
        {
            // End() must be called before commands can be submitted for execution.
            CommandList.End();
            Device.SubmitCommands(CommandList);
        }

        public void Draw()
        {
            SceneGraph.Draw();
        }


        public static Vector2 GetCenterScreenPos(Vector2 size)
        {
            uint width = Device.SwapchainFramebuffer.Width;
            uint height = Device.SwapchainFramebuffer.Height;

            Vector2 screenSize = new Vector2(width, height);
            return (screenSize - size) / 2f;
        }
    }
}
