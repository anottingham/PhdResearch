using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ViewTimeline.Graphs
{
    public class CanvasCollection
    {
        /// <summary>
        /// List of canvases held in the collection
        /// </summary>
        public List<GraphCanvas> List { get; private set; }

        /// <summary>
        /// Constructs an empty canvas collection object
        /// </summary>
        public CanvasCollection()
        {
            List = new List<GraphCanvas>();
        }

        /// <summary>
        /// Clears all graph canvases from the list
        /// </summary>
        public void Clear()
        {
            List.Clear();
        }

        /// <summary>
        /// Adds a graph canvas to the canvas list
        /// </summary>
        /// <param name="canvas">the graph canvas to add</param>
        public void Add(GraphCanvas canvas)
        {
            List.Add(canvas);
        }

        /// <summary>
        /// Moves the draw position of a canvas closer to the foreground
        /// </summary>
        /// <param name="index">the index of the canvas to move forward</param>
        public void MoveForward(int index)
        {
            if (index < List.Count - 1)
            {
                SwapDrawPosition(index, index + 1);
            }
        }

        /// <summary>
        /// Moves the draw position of a canvas further to the background
        /// </summary>
        /// <param name="index">the index of the canvas to move backward</param>
        public void MoveBackward(int index)
        {
            if (index > 0)
            {
                SwapDrawPosition(index, index - 1);
            }
        }

        /// <summary>
        /// Swaps the draw positions of two canvases in the canvas list
        /// </summary>
        /// <param name="index1">the first canvas</param>
        /// <param name="index2">the second canvas</param>
        public void SwapDrawPosition(int index1, int index2)
        {
            GraphCanvas tmp = List[index1];
            List[index1] = List[index2];
            List[index2] = tmp;
        }

        /// <summary>
        /// Sets the alpha blend for all canvases in the canvas list
        /// </summary>
        /// <param name="alpha">the alpha blend value, between 0f and 1f</param>
        public void AlphaBlend(float alpha)
        {
            foreach (var graph in List)
            {
                graph.AlphaBlend = alpha;
            }
        }

        /// <summary>
        /// Swaps the draw buffers of each canvas in the list
        /// </summary>
        public void SwapBuffers()
        {
            foreach (var graphCanvas in List)
            {
                graphCanvas.SwapBuffers();
            }
        }

        /// <summary>
        /// Draws all graphs in the collection, in draw order
        /// </summary>
        /// <param name="scaleData">the scale data used to draw the graphs</param>
        public void DrawCollection(GraphScaleData scaleData)
        {
            foreach (var graph in List)
            {
                graph.Draw(scaleData);
            }
        }

        /// <summary>
        /// Draws the backbuffers of all graphs in the collection, in draw order
        /// </summary>
        /// <param name="scaleData">the scale data used to draw the graphs</param>
        public void DrawCollectionBackBuffer(GraphScaleData scaleData)
        {
            foreach (var graph in List)
            {
                graph.DrawBackBuffer(scaleData);
            }
        }
    }
}