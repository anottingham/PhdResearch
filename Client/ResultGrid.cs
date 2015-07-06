using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ViewReader
{
    public partial class ResultGrid : UserControl
    {
        private bool _enableCrosshair = false;
        private bool _enableScrollLine = false;

        private Point _viewPos;
        private Point _scrollPos;
        private ulong _delta = 0;
        private bool _accelerate;
        private bool _scrollClick = false;

        private Size _pageSize;
        private readonly bool[,] _page;

        private Size _cellSize;
        private Size _elemSize;
        private Size _margin;

        private float _fontSize;

        private FileBuffer _buffer;

        private string _msg;

        public ResultGrid()
        {
            InitializeComponent();
            _pageSize = new Size(32, 16);
            _cellSize = new Size(32, 32);
            _elemSize = new Size(16, 16);
            _margin = new Size(256, 64);
            _fontSize = 12f;
            _page = new bool[_pageSize.Height, _pageSize.Width];

            DoubleBuffered = true;

            _delta = 0;
            BackColor = Color.White;
            _accelerate = false;

            _msg = "No View File Specified.";
        }

        public void LoadView(string view)
        {
            _msg = "Loading...";
            Invalidate();
            SuspendLayout();
            _buffer = new FileBuffer(view);
            ResumeLayout();
        }

        private void DrawCrosshair(PaintEventArgs e)
        {
            ulong start_line = _delta;
            Rectangle rect_vert = new Rectangle(_viewPos.X - 8, _margin.Height - 8, _cellSize.Width, _cellSize.Height * _pageSize.Height);
            Rectangle rect_hor = new Rectangle(8, _viewPos.Y - 8, _margin.Width - 16 + _cellSize.Width * _pageSize.Width, _cellSize.Height);

            Size targetSize = new Size((_cellSize.Width + _elemSize.Width) / 2, (_cellSize.Height + _elemSize.Height) / 2);

            Rectangle rect_target = new Rectangle(_viewPos.X - 8 + (_cellSize.Width - targetSize.Width) / 2, _viewPos.Y - 8 + (_cellSize.Height - targetSize.Height) / 2, targetSize.Width, targetSize.Height);

            //crosshair
            e.Graphics.FillRectangle(Brushes.LightSteelBlue, rect_vert);
            e.Graphics.FillRectangle(Brushes.LightSteelBlue, rect_hor);
            e.Graphics.FillRectangle(Brushes.Black, rect_target);

            //column header
            string str = Convert.ToString((_viewPos.X - _margin.Width + 8) / _cellSize.Width);
            if ((_viewPos.X - _margin.Width + 8) / _cellSize.Width < 10) str = "0" + str;
            e.Graphics.DrawString(str, new Font(FontFamily.GenericMonospace, _fontSize, FontStyle.Bold), Brushes.Black,
                                  new RectangleF(_viewPos.X + 4, 16f, 32f, 32f), StringFormat.GenericTypographic);

            //row header
            str = IntToStr(Convert.ToUInt64(_pageSize.Width) * (start_line + Convert.ToUInt64((_viewPos.Y - _margin.Height + 8) / _cellSize.Height)));
            e.Graphics.DrawString(str, new Font(FontFamily.GenericMonospace, _fontSize, FontStyle.Bold), Brushes.Black,
                                  new RectangleF(8f, _viewPos.Y, 192f - 16f, 32f), StringFormat.GenericTypographic);

            //mouse position
            str = "Selected Packet:      " +
                  IntToStr(Convert.ToUInt64(_pageSize.Width) * (start_line + Convert.ToUInt64(((_viewPos.Y - 56) / _cellSize.Height))) +
                           Convert.ToUInt64((_viewPos.X - _margin.Width + 8) / _cellSize.Width));
            e.Graphics.DrawString(str, new Font(FontFamily.GenericMonospace, _fontSize, FontStyle.Bold), Brushes.Black,
                                  new RectangleF(_margin.Width + _cellSize.Width * 18 - (_cellSize.Width / 2), 64 + _pageSize.Height * _cellSize.Height, 32 * 16, 32f),
                                  StringFormat.GenericTypographic);
        }

        private void DrawScrollBar(PaintEventArgs e)
        {
            //draw main line
            e.Graphics.DrawLine(new Pen(Brushes.Black, 3f),
                _margin.Width + _cellSize.Width * _pageSize.Width + 32,
                _margin.Height - 8,
                _margin.Width + _cellSize.Width * _pageSize.Width + 32,
                _margin.Height + _cellSize.Height * _pageSize.Height - 8);

            //draw start and stop line
            e.Graphics.DrawLine(new Pen(Brushes.Black, 3f),
                _margin.Width + _cellSize.Width * _pageSize.Width + 16,
                _margin.Height - 8,
                _margin.Width + _cellSize.Width * _pageSize.Width + 48,
                _margin.Height - 8);

            e.Graphics.DrawLine(new Pen(Brushes.Black, 3f),
                _margin.Width + _cellSize.Width * _pageSize.Width + 16,
                _margin.Height + _cellSize.Height * _pageSize.Height - 8,
                _margin.Width + _cellSize.Width * _pageSize.Width + 48,
                _margin.Height + _cellSize.Height * _pageSize.Height - 8);

            //draw dividers
            e.Graphics.DrawLine(new Pen(Brushes.Black, 3f),     // 50%
                _margin.Width + _cellSize.Width * _pageSize.Width + 20,
                _margin.Height + (_cellSize.Height / 2) * _pageSize.Height - 8,
                _margin.Width + _cellSize.Width * _pageSize.Width + 44,
                _margin.Height + (_cellSize.Height / 2) * _pageSize.Height - 8);

            e.Graphics.DrawLine(new Pen(Brushes.Black, 3f),     // 25%
                _margin.Width + _cellSize.Width * _pageSize.Width + 24,
                _margin.Height + (_cellSize.Height / 4) * _pageSize.Height - 8,
                _margin.Width + _cellSize.Width * _pageSize.Width + 40,
                _margin.Height + (_cellSize.Height / 4) * _pageSize.Height - 8);

            e.Graphics.DrawLine(new Pen(Brushes.Black, 3f),     // 75%
                _margin.Width + _cellSize.Width * _pageSize.Width + 24,
                _margin.Height + 3 * (_cellSize.Height / 4) * _pageSize.Height - 8,
                _margin.Width + _cellSize.Width * _pageSize.Width + 40,
                _margin.Height + 3 * (_cellSize.Height / 4) * _pageSize.Height - 8);

            float percentage = (32f * (float)_delta) / (float)_buffer.PacketCount;
            float length = _cellSize.Height * _pageSize.Height;

            //draw indicator
            e.Graphics.DrawLine(new Pen(Brushes.SteelBlue, 5f),
                _margin.Width + _cellSize.Width * _pageSize.Width + 16,
                _margin.Height + (int)(percentage * length) - 8,
                _margin.Width + _cellSize.Width * _pageSize.Width + 48,
                _margin.Height + (int)(percentage * length) - 8);

            if (_scrollClick)
            {
                float mousePercent = (((float)(_scrollPos.Y) - _margin.Height + 8) / (_cellSize.Height * _pageSize.Height));
                if (mousePercent < 0) mousePercent = 0f;
                else if (mousePercent >= 1f) mousePercent = 1f;
                _delta = Convert.ToUInt64(((double)(_buffer.PacketCount / (double)_pageSize.Width) * (double)mousePercent));
            }
            else if (_enableScrollLine)
            {
                int delta_pos = (int)(percentage * length) + _margin.Height - 8;
                if (delta_pos - 10 < _scrollPos.Y && delta_pos + 10 > _scrollPos.Y) //over delta position
                {
                    e.Graphics.FillEllipse(Brushes.SteelBlue,
                        _scrollPos.X - 5,
                        delta_pos - 5,
                        10,
                        10);
                }
                else
                    e.Graphics.DrawLine(new Pen(Brushes.Gray, 2f),
                        _margin.Width + _cellSize.Width * _pageSize.Width + 16,
                        _scrollPos.Y,
                        _margin.Width + _cellSize.Width * _pageSize.Width + 48,
                        _scrollPos.Y);
            }
        }

        private void DrawGrid(PaintEventArgs e)
        {
            if (_enableCrosshair)
            {
                DrawCrosshair(e);
            }

            int posY = _margin.Height;
            int posX = _margin.Width;

            ulong startVal = _delta * Convert.ToUInt64(_pageSize.Width);
            ExtractPage(_buffer.Buffer, startVal / 8);

            for (int k = 0; k < 16; ++k)
            {
                string str = IntToStr(startVal + Convert.ToUInt64(_pageSize.Width) * (ulong)k);
                e.Graphics.DrawString(str, new Font(FontFamily.GenericMonospace, _fontSize), Brushes.Black,
                                      new RectangleF(8f, _margin.Height + k * _pageSize.Width, _margin.Width - 16f, _cellSize.Height), StringFormat.GenericTypographic);
                //_mouse_pos.X, 32);
                for (int j = 0; j < 32; ++j)
                {
                    if (startVal + Convert.ToUInt64(k * _pageSize.Width + j) >= _buffer.PacketCount)
                        e.Graphics.FillRectangle(Brushes.White, posX, posY, _elemSize.Width, _elemSize.Height);
                    else if (_page[k, j])
                    {
                        e.Graphics.FillRectangle(Brushes.DarkBlue, posX, posY, _elemSize.Width, _elemSize.Height);
                        e.Graphics.FillRectangle(Brushes.SteelBlue, posX + 2, posY + 2, _elemSize.Width - 4,
                                                 _elemSize.Height - 4);
                    }
                    else
                    {
                        e.Graphics.FillRectangle(Brushes.Red, posX, posY, _elemSize.Width, _elemSize.Height);
                        e.Graphics.FillRectangle(Brushes.White, posX + 2, posY + 2, _elemSize.Width - 4,
                                                 _elemSize.Height - 4);
                    }
                    posX += _cellSize.Width;
                }
                posY += _cellSize.Height;
                posX = _margin.Width;
            }

            //draw grid over top
            Pen p = new Pen(Brushes.Gray);
            p.Width = 2.0f;
            p.LineJoin = LineJoin.Miter;
            for (int k = 0; k <= 32; k += 8)
            {
                //vertical
                e.Graphics.DrawLine(p,
                    _margin.Width + k * _cellSize.Width - 8,
                    _margin.Height - 8,
                    _margin.Width + k * _cellSize.Width - 8,
                    _pageSize.Height * _cellSize.Height + _margin.Height - 8);
            }
            /*for (int k = 0; k <= 16; ++k)
            {
                //horizontal
                e.Graphics.DrawLine(p, offset_x, offset_y + k * rect_size.Height, 32 * rect_size.Height + offset_x, offset_y + k * rect_size.Height);
            }*/
        }

        private void ExtractPage(byte[] buffer, ulong startByte)
        {
            for (int k = 0; k < _pageSize.Height; ++k)
            {
                for (int j = 0; j < _pageSize.Width; ++j)
                {
                    byte mask = (byte)(0x80 >> (j % 8));
                    _page[k, j] = ((buffer[startByte + Convert.ToUInt64(k * (_pageSize.Width / 8) + (j / 8))] & mask) > 0);
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _enableCrosshair = true;
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _enableCrosshair = false;
            _viewPos = Point.Empty;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (e.X > _margin.Width + _pageSize.Width * _cellSize.Width + 15 && e.Y > _margin.Height - 8 && e.X < _margin.Width + _pageSize.Width * _cellSize.Width + 48 && e.Y < _margin.Height - 8 + _cellSize.Height * _pageSize.Height) //scroll bar
                {
                    _scrollClick = true;
                    _scrollPos = e.Location;
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _scrollClick = false;
            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            SuspendLayout();

            _enableCrosshair = false;
            _enableScrollLine = false;

            if (_scrollClick || (e.X > _margin.Width + _pageSize.Width * _cellSize.Width + 15 && e.Y > _margin.Height - 8 && e.X < _margin.Width + _pageSize.Width * _cellSize.Width + 48 && e.Y < _margin.Height - 8 + _cellSize.Height * _pageSize.Height))
            {
                _enableScrollLine = true;
                _scrollPos = e.Location;
            }
            else if (e.X > _margin.Width - 1 && e.Y > _margin.Height - 1 && e.X < _margin.Width + _pageSize.Width * _cellSize.Width && e.Y < _margin.Height + _pageSize.Height * _cellSize.Height) //in main view
            {
                _enableCrosshair = true;
                _viewPos = new Point((e.X / _cellSize.Width) * _cellSize.Width, (e.Y / _cellSize.Height) * _cellSize.Height);
            }
            ResumeLayout();
            this.Invalidate();

            base.OnMouseMove(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            long val = Convert.ToInt64(_delta) - Convert.ToInt64(_accelerate ? e.Delta / 8 : e.Delta / 120);
            if (val < 0) _delta = 0;
            else if (Convert.ToUInt64(val) < _buffer.PacketCount / 32) _delta = Convert.ToUInt64(val);

            Invalidate();
            base.OnMouseWheel(e);
        }

        protected override void OnPaint(PaintEventArgs args)
        {
            if (_buffer != null)
            {
                DrawGrid(args);
                DrawScrollBar(args);
            }
            else
            {
                Font font = new Font(FontFamily.GenericMonospace, 16f);
                SizeF sz = args.Graphics.MeasureString(_msg, font);
                args.Graphics.DrawString(_msg, font, Brushes.Black, (Width / 2) - (sz.Width / 2), (Height / 2) - (sz.Height / 2));
            }
            ;
            base.OnPaint(args);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Control) _accelerate = true;
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (!e.Control) _accelerate = false;
            base.OnKeyUp(e);
        }

        private string IntToStr(ulong value)
        {
            int width = 12;
            string val = "";
            for (int k = 1; k <= width; k++)
            {
                if (value < Math.Pow(10, k)) val += " ";
            }
            return val + value.ToString(CultureInfo.InvariantCulture);
        }
    }
}