using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ViewTimeline.FieldSet
{
    public partial class FieldSetControl : UserControl
    {
        
        private enum FieldSetControlState
        {
            Empty,
            Loading,
            Full
        };

        private FieldSetData data;
        private List<FieldSetRecord> records;
        private int unmatched;

        private FieldSetControlState state;

        private int yPos;

        private Size _recordSize;
        private int _barWidth;
        private Rectangle _viewPort;

        private Timer timer;

        public FieldSetControl()
        {
            state = FieldSetControlState.Empty;
            yPos = 0;
            InitializeComponent();
            timer = new Timer();
            timer.Tick += timer_Tick;
            timer.Interval = 100;
            this.DoubleBuffered = true;
        }
        
        void timer_Tick(object sender, EventArgs e)
        {
            Invalidate();
        }

        public async void Initialise(string filename)
        {
            state = FieldSetControlState.Loading;
            timer.Start();
            data = new FieldSetData();
            await data.Initialise(filename);

            timer.Stop();
            records = data.GetValidRecords;
            records.Sort();
            
            Height = records.Count * 50 + 400;
            state = FieldSetControlState.Full;
            this.Invalidate();
        }

        public void SetContainerSize(Size size)
        {
            _recordSize = new Size(800, 14);
            
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.White);

            switch (state)
            {
                case FieldSetControlState.Empty:
                    e.Graphics.DrawString("Empty", new Font(FontFamily.GenericMonospace, 36f), Brushes.Black, new RectangleF(200, 300, 200, 50));
                    break;
                case FieldSetControlState.Loading:
                    DrawLoading(e);
                    break;
                case FieldSetControlState.Full:
                    DrawChart(e);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            base.OnPaint(e);
        }

        private void DrawLoading(PaintEventArgs e)
        {
            var gfx = e.Graphics;
            gfx.FillRectangle(Brushes.LightSlateGray, new Rectangle(190, 190, 420, 120));

            int width = Convert.ToInt32(data.Completion * 400);
            gfx.FillRectangle(Brushes.SteelBlue, new Rectangle(200, 200, width, 100));
        }

        private void DrawChart(PaintEventArgs e)
        {
            int y = 50;

            for (int k = 0; k < records.Count; k++)
            {
                DrawBar(e.Graphics, records[k], y);
                y += 14;
            }
        }

        private void DrawBar(Graphics gfx, FieldSetRecord record, int yOffset)
        {
            gfx.DrawString(record.Value.ToString("n0"), new Font(FontFamily.GenericMonospace, 8f), Brushes.Black, new RectangleF(20, yOffset, 100, 25));
            gfx.FillRectangle(Brushes.LightSteelBlue, new Rectangle(100, yOffset, 300, 10));
            int width = Convert.ToInt32(record.ValidTrafficPercent * 3);
            gfx.FillRectangle(Brushes.SteelBlue, new Rectangle(100, yOffset, width, 10));
            gfx.DrawString(record.ValidTrafficPercent.ToString("f2") + "%\t(" + record.TrafficPercent.ToString("f2") + "%)\t" + record.Count.ToString("n0") + " packets", new Font(FontFamily.GenericMonospace, 8f), Brushes.Black, new RectangleF(520, yOffset, 400, 25));
        }

       
    }

}
