using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ViewReader
{
    internal class FileBuffer
    {
        private ulong packet_count;

        private byte[] _buffer;

        public FileBuffer(string view_file)
        {
            FileStream viewFile = new FileStream(view_file, FileMode.Open, FileAccess.Read);
            _buffer = new byte[viewFile.Length]; //add extra incase of overflow
            byte[] buffer = new byte[8];
            viewFile.Seek(8, SeekOrigin.Begin);
            viewFile.Read(buffer, 0, 8);
            viewFile.Seek(272, SeekOrigin.Begin);
            packet_count = Convert.ToUInt64(BitConverter.ToInt64(buffer, 0));

            viewFile.Read(_buffer, 0, (int)viewFile.Length - 272);

            viewFile.Close();
        }

        public byte[] Buffer
        {
            get { return _buffer; }
        }

        public ulong PacketCount
        {
            get { return packet_count; }
        }
    }
}