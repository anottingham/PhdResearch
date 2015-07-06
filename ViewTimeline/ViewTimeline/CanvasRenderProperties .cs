using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ViewTimeline.Graphs;

namespace ViewTimeline
{
    public partial class CanvasRenderProperties : Form
    {
        private GraphCanvas _canvas;

        private GraphScaleFunction selectedFunction;
        private GraphScaleTarget selectedTarget;
        private GraphType selectedType;

        private int filter;

        public CanvasRenderProperties()
        {
            InitializeComponent();
        }
        

        public void Initialise(GraphCanvas canvas, List<string> filters)
        {
            _canvas = canvas;
            cbGraphType.Items.AddRange(new object[] { "Line Graph", "Scatter Plot", "Solid Graph" });
            cbScaleFunction.Items.AddRange(new object[] { "Maximum", "Average"});
            cbScaleTarget.Items.AddRange(new object[] {"Data Volume", "Packet Count"});

            foreach (var f in filters)
            {
                cbScaleTarget.Items.Add("Filter: " + f);
            }

            selectedType = _canvas.GraphType;
            selectedFunction = _canvas.ScaleFunction.Function;
            selectedTarget = _canvas.ScaleFunction.Target;
            filter = _canvas.ScaleFunction.FilterIndex;

            switch (_canvas.GraphType)
            {
                case GraphType.LineGraph:
                    cbGraphType.SelectedIndex = 0;
                    break;
                case GraphType.ScatterPlot:
                    cbGraphType.SelectedIndex = 1;
                    break;
                case GraphType.SolidGraph:
                    cbGraphType.SelectedIndex = 2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }


            switch (_canvas.ScaleFunction.Function)
            {
                case GraphScaleFunction.Maximum:
                    cbScaleFunction.SelectedIndex = 0;
                    break;
                case GraphScaleFunction.AverageOver2:
                    cbScaleFunction.SelectedIndex = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (_canvas.ScaleFunction.Target)
            {
                case GraphScaleTarget.DataVolume:
                    cbScaleTarget.SelectedIndex = 0;
                    break;
                case GraphScaleTarget.PacketCount:
                    cbScaleTarget.SelectedIndex = 1;
                    break;
                case GraphScaleTarget.MatchingCount:
                    cbScaleTarget.SelectedIndex = 2 + filter;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void cbGraphType_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cbGraphType.SelectedIndex)
            {
                case 0:
                    selectedType = GraphType.LineGraph;
                    break;
                case 1:
                    selectedType = GraphType.ScatterPlot;
                    break;
                case 2:
                    selectedType = GraphType.SolidGraph;
                    break;
            }
        }

        private void cbScaleFunction_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cbScaleFunction.SelectedIndex)
            {
                case 0:
                    selectedFunction = GraphScaleFunction.Maximum;
                    break;
                case 1:
                    selectedFunction = GraphScaleFunction.AverageOver2;
                    break;
            }
        }

        private void cbScaleTarget_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cbScaleTarget.SelectedIndex)
            {
                case 0:
                    selectedTarget = GraphScaleTarget.DataVolume;
                    filter = 0;
                    break;
                case 1:
                    selectedTarget = GraphScaleTarget.PacketCount;
                    filter = 0;
                    break;
                default:
                    selectedTarget = GraphScaleTarget.MatchingCount;
                    filter = cbScaleTarget.SelectedIndex - 2;
                    break;
            }
        }


        private void btnApply_Click(object sender, EventArgs e)
        {
            _canvas.GraphType = selectedType;
            _canvas.ScaleFunction = new GraphScaleConfig(selectedFunction, selectedTarget, filter);
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }

}
