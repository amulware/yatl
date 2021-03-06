using System;
using System.Diagnostics;
using amulware.Graphics;
using OpenTK.Graphics.OpenGL;
using yatl.Environment;
using yatl.Utilities;

namespace yatl.Rendering
{
    sealed class GameRenderer
    {
        private int screenWidth;
        private int screenHeight;
        private float aspectRatio;
        private int scissorX;
        private int scissorY;
        private int scissorW;
        private int scissorH;

        private readonly ShaderManager shaders;
        private readonly SurfaceManager surfaces;
        private readonly SpriteManager sprites;

        private Stopwatch shaderReloadTimer;
        private DeferredBuffer deferredBuffer;
        private PostProcessSurface debugDeferred;

        private PostProcessSurface copyLightToScreen;

        public GameRenderer()
        {
            this.deferredBuffer = new DeferredBuffer();
            this.shaders = new ShaderManager();
            this.surfaces = new SurfaceManager(this.shaders, this.deferredBuffer);
            this.sprites = new SpriteManager(this.surfaces);


            this.debugDeferred = new PostProcessSurface();
            this.debugDeferred.AddSettings(this.deferredBuffer);
            this.shaders.DebugDeferred.UseOnSurface(this.debugDeferred);

            this.copyLightToScreen = new PostProcessSurface();
            this.copyLightToScreen.AddSetting(this.deferredBuffer.LightAccumulationTexture);
            this.shaders.PostCopy.UseOnSurface(this.copyLightToScreen);

            this.shaderReloadTimer = Stopwatch.StartNew();
        }

        public void Render(GameState state)
        {
            state.Draw(this.sprites);

            this.surfaces.SetGameCamera(state.Camera);
        }

        public void FinalizeFrame()
        {
            #region Reload Shaders
#if DEBUG
            if (this.shaderReloadTimer.Elapsed > TimeSpan.FromSeconds(0.2))
            {
                GraphicsHelper.CheckAndUpdateChangedShaders();
                this.shaderReloadTimer.Restart();
            }
#endif
            #endregion

            #region Global Settings

            GL.Disable(EnableCap.CullFace);

            #endregion

            
            #region Draw Game

            #region Set and clear deferred buffer

            this.deferredBuffer.BindDeferred();

            GL.Viewport(0, 0, this.scissorW, this.scissorH);
            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            #endregion

            #region Draw deferred geometry

            GL.DepthMask(true);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);

            //this.surfaces.Walls.Render();
            foreach (var surface in this.surfaces.LevelGeometryQueue)
                surface.Render();

            GL.DepthMask(false);


            #endregion

            #region Set and clear light accumulation buffer

            this.deferredBuffer.BindLightAccumulation();

            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit);


            #endregion

            #region Draw lights
            
            //this.debugDeferred.Render();

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Front);

            this.surfaces.PointLights.Render();

            foreach (var surface in this.surfaces.LightQueue)
                surface.Render();

            GL.Disable(EnableCap.CullFace);

            #endregion
            
            #region Draw particles

            this.surfaces.Particles.Surface.Render();

            this.surfaces.Sprites.Surface.Render();
            
            #endregion

            this.surfaces.Overlay.Render();

            #region set and clear backbuffer

            GL.DepthMask(true);

            GL.Disable(EnableCap.DepthTest);

            this.deferredBuffer.Unbind();


            GL.Viewport(0, 0, this.screenWidth, this.screenHeight);

            GL.ClearColor(Color.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


            GL.Viewport(this.scissorX, this.scissorY, this.scissorW, this.scissorH);
            GL.Scissor(this.scissorX, this.scissorY, this.scissorW, this.scissorH);

            GL.Enable(EnableCap.ScissorTest);
            GL.ClearColor(Color.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Disable(EnableCap.ScissorTest);

            #endregion

            #region copy light to backbuffer

            this.copyLightToScreen.Render();


            #endregion

            #region Draw Debug

            this.surfaces.Hexagons.Surface.Render();

            this.surfaces.GameFontSurface.Render();

            #endregion

            #endregion

            #region Draw Interface

            this.surfaces.Hud.Surface.Render();

            this.surfaces.Tutorial.Surface.Render();

            this.surfaces.ScreenFontSurface.Render();

            #endregion

            this.surfaces.ClearQueues();
        }

        public void Resize(int width, int height)
        {
            this.screenWidth = width;
            this.screenHeight = height;

            this.aspectRatio = (float)width / height;

            if (this.aspectRatio >= 16.0 / 9.0)
            {
                int w = (int)(height * 16.0 / 9.0);
                int x = (width - w) / 2;

                this.scissorX = x;
                this.scissorY = 0;
                this.scissorW = w;
                this.scissorH = this.screenHeight;
            }
            else
            {
                int h = (int)(width * 9.0 / 16.0);
                int y = (height - h) / 2;

                this.scissorX = 0;
                this.scissorY = y;
                this.scissorW = this.screenWidth;
                this.scissorH = h;
            }

            this.deferredBuffer.Resize(this.scissorW, this.scissorH);
        }
    }
}
