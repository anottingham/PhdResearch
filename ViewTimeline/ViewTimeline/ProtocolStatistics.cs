using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ViewTimeline.Graphs;

namespace ViewTimeline
{
    public partial class ProtocolStatistics : Form
    {
        private FilterStatistics _stats;
        public ProtocolStatistics()
        {
            InitializeComponent();
            _stats = CanvasManager.FileManager.CountCache.GenerateStatistics(
                ViewForm.TrimFilenames(CanvasManager.FileManager.CountCache.FilterFiles.Select(f => f.Filename).ToList())
                    .ToArray());
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics gfx;
            Font fnt;// = new Font(FontFamily.GenericMonospace, 12f);
            SizeF size; //= gfx.MeasureString(CanvasManager.CurrentCanvasData.Description, fnt);
               
            gfx = panel1.CreateGraphics();
            gfx.Clear(panel1.BackColor);
            fnt = new Font(FontFamily.GenericMonospace, 10f);

            List<string> output = new List<string>();

            output.Add("Total packets:");
            output.Add(_stats.PacketCount.ToString("#,0", CultureInfo.InvariantCulture));
            output.Add("( 100% )");

            for (int k = 0; k < _stats.ProtocolCount; k++)
            {
                output.Add(_stats.ProtocolNames[k] + " packets:");
                output.Add(_stats.MatchingCount[k].ToString("#,0", CultureInfo.InvariantCulture));
                output.Add("( " + _stats.MatchingRatio[k].ToString("#,0", CultureInfo.InvariantCulture) + "% )");
            }

            float yPos = 20f;
            for (int k = 0; k <= _stats.ProtocolCount * 3; k += 3) 
            {
                size = gfx.MeasureString(output[k], fnt);
                gfx.DrawString(output[k], fnt, Brushes.Black, new RectangleF(20f, yPos, size.Width, size.Height));

                size = gfx.MeasureString(output[k + 1], fnt);
                gfx.DrawString(output[k + 1], fnt, Brushes.Black, new RectangleF(250f, yPos, size.Width, size.Height));

                size = gfx.MeasureString(output[k + 2], fnt);
                gfx.DrawString(output[k + 2], fnt, Brushes.Black, new RectangleF(400f, yPos, size.Width, size.Height));
                
                yPos += size.Height + 5f;
            }

            base.OnPaint(e);
        }
    }

}
