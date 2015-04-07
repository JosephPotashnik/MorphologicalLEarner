using System; 
using System.Windows; 
using System.Collections.Generic;
using MorphologicalLearner;
using Smrf.NodeXL.Core; 
using Smrf.NodeXL.Algorithms; 
using Smrf.NodeXL.Layouts;
using Smrf.NodeXL.Visualization.Wpf;



namespace GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Learner learner;

        public MainWindow()
        {
            InitializeComponent();

            learner = new Learner("David Copperfield");

            CreateAndShowGroups();
        }

        private IVertex GetorAddVertex(string word, Dictionary<string, IVertex> addedVertices, IGraph graph)
        {
            IVertex ver;
            if (!addedVertices.ContainsKey(word))
            {
                ver = new Vertex();
                addedVertices[word] = ver;

                ver.SetValue(ReservedMetadataKeys.PerVertexShape,
           VertexShape.Label);

                ver.SetValue(ReservedMetadataKeys.PerVertexLabel, word);



                graph.Vertices.Add(ver);
            }
            else
                ver = addedVertices[word];

            return ver;
        }
        private IGraph ReadLogicalGraph(Dictionary<string, Dictionary<string, int>> g)
        {
             IGraph graph = new Graph(GraphDirectedness.Undirected);
            try
            {
                var addedVertices = new Dictionary<string, IVertex>();

                Dictionary<string, Dictionary<string, int>> dic = new Dictionary<string, Dictionary<string, int>>();
                foreach (var word1 in g.Keys)
                {
                    foreach (var word2 in g[word1].Keys)
                    {
                        if (word1 == word2)
                            continue;

                        //if we already scanned these words in the opposite order, skip.
                        if (dic.ContainsKey(word2) && dic[word2].ContainsKey(word1))
                            continue;

                        if (!dic.ContainsKey(word1))
                            dic[word1] = new Dictionary<string, int>();

                        //push into dictionary to keep track of scanned pairs.
                        dic[word1][word2] = 1;

                        IVertex ver1 = GetorAddVertex(word1, addedVertices, graph);
                        IVertex ver2 = GetorAddVertex(word2, addedVertices, graph);
                        
                        IEdge e = graph.Edges.Add(ver1, ver2);
                        //e.SetValue(ReservedMetadataKeys.EdgeWeight, g[word1][word2]);

                    }
                }

               // graphs[i].Attr.LayerDirection = LayerDirection.LR;

               // graphs[i].LayoutAlgorithmSettings.EdgeRoutingSettings.RouteMultiEdgesAsBundles = true;
                //graph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.SplineBundling;
                //layout the graph and draw it
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return graph;
        }

        private void CreateAndShowGroups()
        {
            // Create a new graph.

            learner.Learn();
            var g = learner.NeighborGraph.RightWordsNeighborhoods;

            IGraph graph = ReadLogicalGraph(g);

            // Use a ClusterCalculator to partition the graph's vertices into
            // clusters.

            ClusterCalculator clusterCalculator = new ClusterCalculator();

            ICollection<Community> clusters =
                clusterCalculator.CalculateGraphMetrics(graph);

            // One group will be created for each cluster.

            List<GroupInfo> groups = new List<GroupInfo>();

            foreach (Community cluster in clusters)
            {
                // Create a group.

                GroupInfo group = new GroupInfo();
                groups.Add(group);

                // Populate the group with the cluster's vertices.
                foreach (IVertex vertex in cluster.Vertices)
                {
                    group.Vertices.AddLast(vertex);
                }
            }

            // Store the group information as metadata on the graph.
            graph.SetValue(ReservedMetadataKeys.GroupInfo, groups.ToArray());

            // Assign the graph to the NodeXLControl.
            nodeXLControl1.Graph = graph;

            // Tell the layout class to use the groups.
            nodeXLControl1.Layout.LayoutStyle = LayoutStyle.UseGroups;

            // Tell the layout class how to lay out the groups.
            nodeXLControl1.Layout.BoxLayoutAlgorithm =
                BoxLayoutAlgorithm.ForceDirected;

            // Tell the NodeXLControl to lay out and draw the graph.
            nodeXLControl1.DrawGraph(true);
        }
    }
}
