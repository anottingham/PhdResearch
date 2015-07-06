using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace ViewTimeline.GLControls
{
    public enum GLSLParameterType
    {
        Attribute,
        Uniform
    }

    public class ShaderProgram
    {
        private readonly int _vertexShaderHandle;
        private readonly int _fragmentShaderHandle;
        private readonly int _shaderProgramHandle;

        public int ShaderProgramHandle
        {
            get { return _shaderProgramHandle; }
        }

        private readonly string _vertexShaderSource;
        private readonly string _fragmentShaderSource;

        public ShaderProgram(string vShader, string fShader)
        {
            using (FileStream stream = new FileStream(vShader, FileMode.Open))
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
                _vertexShaderSource = "\n" + Encoding.UTF8.GetString(buffer);
                //Debug.WriteLine(_vertexShaderSource);
            }
            using (FileStream stream = new FileStream(fShader, FileMode.Open))
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
                _fragmentShaderSource = "\n" + Encoding.UTF8.GetString(buffer);
                //Debug.WriteLine(_fragmentShaderSource);
            }

            _vertexShaderHandle = GL.CreateShader(ShaderType.VertexShader);
            _fragmentShaderHandle = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(_vertexShaderHandle, _vertexShaderSource);
            GL.ShaderSource(_fragmentShaderHandle, _fragmentShaderSource);

            GL.CompileShader(_vertexShaderHandle);
            GL.CompileShader(_fragmentShaderHandle);

            _shaderProgramHandle = GL.CreateProgram();

            GL.AttachShader(_shaderProgramHandle, _vertexShaderHandle);
            GL.AttachShader(_shaderProgramHandle, _fragmentShaderHandle);

            GL.LinkProgram(_shaderProgramHandle);

            int linkOk;
            GL.GetProgram(_shaderProgramHandle, ProgramParameter.LinkStatus, out linkOk);
            if (linkOk == 0)
            {
                throw new Exception("Link shader program failed.");
            }
            string programInfoLog;
            GL.GetProgramInfoLog(_shaderProgramHandle, out programInfoLog);

            Debug.WriteLine(programInfoLog);
        }

        public int GetLocation(string name, GLSLParameterType type)
        {
            int loc = -1;
            switch (type)
            {
                case GLSLParameterType.Attribute:
                    loc = GL.GetAttribLocation(ShaderProgramHandle, name);
                    break;
                case GLSLParameterType.Uniform:
                    loc = GL.GetUniformLocation(ShaderProgramHandle, name);
                    break;
            }

            if (loc == -1) throw new Exception("could not bind " + name + " to shader program");
            return loc;
        }

        public void UseProgram()
        {
            GL.UseProgram(_shaderProgramHandle);
        }
    }
}