using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Xml.Serialization;
using GraphX;
using GraphX.GraphSharp.Algorithms.Layout.Simple.FDP;
using GraphX.GraphSharp.Algorithms.OverlapRemoval;
using GraphX.Logic;
using GraphX.Logic.Models;
using QuickGraph;

namespace LearnerGUI
{
    //Layout visual object
    public class GraphAreaExample : GraphArea<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>> { }

    //Graph data object
    public class GraphExample : BidirectionalGraph<DataVertex, DataEdge> { }

    //Vertex data object
    public class DataVertex : VertexBase
    {
        [XmlAttribute("text")]
        [DefaultValue("")]
        public string Text { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }

    //Edge data object
    public class DataEdge : EdgeBase<DataVertex>
    {
        public DataEdge(DataVertex source, DataVertex target, double weight = 1)
            : base(source, target, weight)
        {
        }

        public DataEdge()
            : base(null, null, 1)
        {
        }

        [XmlAttribute("text")]
        [DefaultValue("")]
        public string Text { get; set; }

        public override string ToString()
        {
            return Text;
        }

    }
    public partial class MainWindow : Window
    {

        public GraphExample graph;

        public void InitializeComponentGraph(Dictionary<string, Dictionary<string, int>> LogicalGraph)
        {
            graph = new GraphExample();
            foreach (var vertice in LogicalGraph.Keys)
            {
                DataVertex source = new DataVertex {Text = vertice};
                graph.AddVertex(source);

                foreach (var kvp in LogicalGraph[vertice])
                {
                    DataVertex target = new DataVertex {Text = kvp.Key};
                    DataEdge edge = new DataEdge(source, target) {Text = kvp.Value.ToString()};
                    graph.AddVertex(target);
                    graph.AddEdge(edge);
                }              
            }

            var logic = new GXLogicCore<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>>(graph);
            gg_Area.LogicCore = logic;
           
            
        }
    }
}
