using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace ViewTimeline.GLControls
{
    public class GLSLVertexBuffer<TElementType> //: IGLSLVertexBuffer
    {
        private int _cardinality;
        private int _attributeLocation;
        private int _length;

        private int _vertexBufferId;
        private VertexAttribPointerType _type;

        public BufferTarget Target { get; set; }

        public GLSLVertexBuffer()
        {
            _vertexBufferId = -1;
            Target = BufferTarget.ArrayBuffer;

            _attributeLocation = -1;
            _cardinality = 1;
            Normalised = false;
            Stride = 0;

            GL.GenBuffers(1, out _vertexBufferId);
        }

        public void SetAttributeInfo(int attributeLocation, int cardinality, bool normalised = false, int stride = 0)
        {
            if (attributeLocation == -1) throw new Exception("Supplied attribute does not exist, or failed to initialise");
            _attributeLocation = attributeLocation;
            _cardinality = cardinality;
            Normalised = normalised;
            Stride = stride;
            Target = BufferTarget.ArrayBuffer;
        }

        public void SetID(int id)
        {
            _vertexBufferId = id;
        }

        public void BindData(object data, BufferUsageHint hint = BufferUsageHint.StaticDraw)
        {
            if (_vertexBufferId == -1) throw new Exception("No BufferID defined - could not bind data");
            if (data.GetType() == typeof(TElementType[]))
            {
                _length = ((TElementType[])data).Length;
            }
            else if (data.GetType() == typeof(TElementType[,]))
            {
                _length = ((TElementType[,])data).Length;
            }
            else
            {
                throw new Exception("Supplied data is not an array of " + typeof(TElementType) + " elements.");
            }

            int lengthInBytes;

            if (typeof(TElementType) == typeof(Vector2))
            {
                _length *= 2;
                lengthInBytes = _length * sizeof(float);
                _type = VertexAttribPointerType.Float;
            }
            else if (typeof(TElementType) == typeof(Vector3))
            {
                _length *= 3;
                lengthInBytes = _length * sizeof(float);
                _type = VertexAttribPointerType.Float;
            }

            else if (typeof(TElementType) == typeof(float))
            {
                lengthInBytes = _length * sizeof(float);
                _type = VertexAttribPointerType.Float;
            }
            else if (typeof(TElementType) == typeof(double))
            {
                lengthInBytes = _length * sizeof(double);
                _type = VertexAttribPointerType.Double;
            }
            else if (typeof(TElementType) == typeof(int))
            {
                lengthInBytes = _length * sizeof(int);
                _type = VertexAttribPointerType.Int;
            }
            else if (typeof(TElementType) == typeof(short))
            {
                lengthInBytes = _length * sizeof(short);
                _type = VertexAttribPointerType.Short;
            }
            else if (typeof(TElementType) == typeof(byte))
            {
                lengthInBytes = _length * sizeof(byte);
                _type = VertexAttribPointerType.UnsignedByte; //.net bytes are all unsigned
            }
            else if (typeof(TElementType) == typeof(uint))
            {
                lengthInBytes = _length * sizeof(uint);
                _type = VertexAttribPointerType.UnsignedInt;
            }
            else if (typeof(TElementType) == typeof(ushort))
            {
                lengthInBytes = _length * sizeof(ushort);
                _type = VertexAttribPointerType.UnsignedShort;
            }
            else
                throw new Exception("Invalid vertex buffer type - only Vector2, Vector3, float, double, byte, (u)short, (u)int supported");

            GL.BindBuffer(Target, _vertexBufferId);
            IntPtr ptr = GCHandle.Alloc(data, GCHandleType.Pinned).AddrOfPinnedObject();

            GL.BufferData(Target, new IntPtr(lengthInBytes), ptr, hint);
            GL.BindBuffer(Target, 0);

            _length /= _cardinality;
        }

        public int Stride { get; set; }

        public bool Normalised { get; set; }

        public void BeginDraw()
        {
            GL.BindBuffer(Target, _vertexBufferId);
            if (_attributeLocation != -1)
            {
                GL.EnableVertexAttribArray(_attributeLocation);
                GL.VertexAttribPointer(_attributeLocation, _cardinality, _type, Normalised, Stride, 0);
            }
        }

        public void Draw(BeginMode mode)
        {
            GL.DrawArrays(mode, 0, _length);
        }

        public void Draw(BeginMode mode, int length)
        {
            GL.DrawArrays(mode, 0, length);
        }

        public void Draw(BeginMode mode, int startIndex, int length)
        {
            GL.DrawArrays(mode, startIndex, length);
        }

        public void EndDraw()
        {
            GL.DisableVertexAttribArray(_attributeLocation);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
    }

    internal class GLSLIndexBuffer<TElementType>// : IGLSLVertexBuffer
    {
        private int _length;

        private DrawElementsType _type;
        private int _indexBufferId;

        private readonly GLSLVertexBuffer<TElementType> _vertexBuffer;

        public GLSLIndexBuffer()
        {
            GL.GenBuffers(1, out _indexBufferId);
            _vertexBuffer = new GLSLVertexBuffer<TElementType>();
        }

        ~GLSLIndexBuffer()
        {
            if (GL.IsBuffer(_indexBufferId))
            {
                GL.DeleteBuffers(1, ref _indexBufferId);
            }
        }

        public void BindData(object vertexData, object indexData, BufferUsageHint hint = BufferUsageHint.StaticDraw)
        {
            if (_indexBufferId == -1) throw new Exception("No BufferID defined - could not bind data");

            if (!indexData.GetType().IsArray)
            {
                throw new Exception("Supplied index data is not an array.");
            }

            _vertexBuffer.BindData(vertexData, hint);
            _length = ((TElementType[])indexData).Length;

            int lengthInBytes;

            if (indexData.GetType() == typeof(byte[]))
            {
                lengthInBytes = _length * sizeof(byte);
                _type = DrawElementsType.UnsignedByte;
            }
            else if (indexData.GetType() == typeof(ushort[]))
            {
                lengthInBytes = _length * sizeof(ushort);
                _type = DrawElementsType.UnsignedShort;
            }
            else if (indexData.GetType() == typeof(uint[]))
            {
                lengthInBytes = _length * sizeof(uint);
                _type = DrawElementsType.UnsignedInt;
            }
            else
                throw new Exception(
                    "Invalid vertex buffer type - only byte, ushort, uint supported");

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBufferId);
            IntPtr ptr = GCHandle.Alloc(indexData, GCHandleType.Pinned).AddrOfPinnedObject();

            GL.BufferData(BufferTarget.ElementArrayBuffer, new IntPtr(lengthInBytes), ptr, hint);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        public void BeginDraw()
        {
            _vertexBuffer.BeginDraw();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBufferId);
        }

        public void Draw(BeginMode mode)
        {
            GL.DrawElements(mode, _length, _type, IntPtr.Zero);
        }

        public void Draw(BeginMode mode, int count)
        {
            GL.DrawElements(mode, count, _type, IntPtr.Zero);
        }

        public void EndDraw()
        {
            _vertexBuffer.EndDraw();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }
    }
}