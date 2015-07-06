using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ViewTimeline.GLControls
{
    public class SimpleShader2D
    {
        private ShaderProgram _program;

        public int AttributeCoord2D { get; private set; }

        //vertex shader uniforms
        public int UniformTransform { get; private set; }

        //public int UniformHeight { get; private set; }

        //public int UniformWidth { get; private set; }

        //public int UniformXOffset { get; private set; }

        //fragment shader uniforms
        public int UniformColor { get; private set; }

        public SimpleShader2D()
        {
            _program = new ShaderProgram("simple2d.v.glsl", "simple2d.f.glsl");
            AttributeCoord2D = _program.GetLocation("coord2d", GLSLParameterType.Attribute);
            UniformTransform = _program.GetLocation("transform", GLSLParameterType.Uniform);
            //UniformHeight = _program.GetLocation("height", GLSLParameterType.Uniform);
            //UniformWidth = _program.GetLocation("width", GLSLParameterType.Uniform);
            //UniformXOffset = _program.GetLocation("xOffset", GLSLParameterType.Uniform);
            UniformColor = _program.GetLocation("color", GLSLParameterType.Uniform);
        }

        public void UseProgram()
        {
            _program.UseProgram();
        }
    }
}