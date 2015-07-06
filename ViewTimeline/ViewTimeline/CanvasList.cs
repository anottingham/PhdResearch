using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using ViewTimeline.Graphs;

namespace ViewTimeline
{
    public partial class CanvasList : UserControl
    {
        private CanvasCollection _collection;
        private List<string> _filters;

        public CanvasList()
        {
            InitializeComponent();
        }

        public void Initialise(CanvasCollection collection, List<string> filters)
        {
            SuspendLayout();
            this.Controls.Clear();
            _collection = collection;
            _filters = filters;

            for (int index = 0; index < collection.List.Count; index++)
            {
                var prop = new CanvasListItem();

                prop.Location = new Point(125, (prop.Height + 5) * index);
                prop.PositionChanged += prop_PositionChanged;
                Controls.Add(prop);
            }
            RebindGraphs();
            ResumeLayout();
            Invalidate();
        }

        private void prop_PositionChanged(GraphCanvas sender, bool up)
        {
            SuspendLayout();
            int index = _collection.List.IndexOf(sender);
            if (up) _collection.MoveBackward(index);
            else _collection.MoveForward(index);
            RebindGraphs();
            ResumeLayout();
            Invalidate();
        }

        public void RebindGraphs()
        {
            SuspendLayout();
            for (int index = 0; index < _collection.List.Count; index++)
            {
                var control = Controls[index] as CanvasListItem;
                Debug.Assert(control != null, "control != null");
                control.Initialise(_collection.List[index], _filters);

                if (_collection.List.Count == 1)
                {
                    control.EnableMoveButtons(false, false);
                }
                else if (index == 0)
                {
                    control.EnableMoveButtons(false, true);
                }
                else if (index == _collection.List.Count - 1)
                {
                    control.EnableMoveButtons(true, false);
                }
                else control.EnableMoveButtons(true, true);
            }
            ResumeLayout();
        }
    }
}