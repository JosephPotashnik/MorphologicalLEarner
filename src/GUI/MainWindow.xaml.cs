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

        
        

        private void CreateAndShowGroups()
        {
            // Create a new graph.

            learner.Learn();
            IGraph graph = learner.ReadLogicalGraph();

            // Use a ClusterCalculator to partition the graph's vertices into
            // clusters.

            ClusterCalculator clusterCalculator = new ClusterCalculator();
            //clusterCalculator.Algorithm = ClusterAlgorithm.WakitaTsurumi;
            clusterCalculator.Algorithm = ClusterAlgorithm.ClausetNewmanMoore;


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
                BoxLayoutAlgorithm.Treemap;

            // Tell the NodeXLControl to lay out and draw the graph.
            nodeXLControl1.DrawGraph(true);
        }
    }
}
