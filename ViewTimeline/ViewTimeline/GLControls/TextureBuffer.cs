using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace ViewTimeline.GLControls
{
    internal class TextureBuffer
    {
        private int textureId;

        public TextureBuffer(TextureUnit unit, TextureTarget target, byte[,] graph, int width, int height)
        {
            GL.ActiveTexture(unit);
            textureId = GL.GenTexture();
            GL.BindTexture(target, textureId);
            GL.TexImage2D(target, 0, PixelInternalFormat.Luminance, width, height, 0, PixelFormat.Luminance, PixelType.UnsignedByte, graph);
        }

        public int TextureID { get { return textureId; } }
    }
}