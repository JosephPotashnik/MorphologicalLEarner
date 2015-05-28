using System.Collections.Generic;
using System.Windows;
using MorphologicalLearner;
using Smrf.NodeXL.Core;
using Smrf.NodeXL.Layouts;
using Smrf.NodeXL.Visualization.Wpf;
using Community = Smrf.NodeXL.Algorithms.Community;

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
            //learner = new Learner("CorwinBooks");
            CreateAndShowGroups();
        }

        private void CreateAndShowGroups()
        {
            // Create a new graph.

            learner.LookForSyntacticCategoryCandidates();
            ClusterAndDrawGraph(Learner.LocationInBipartiteGraph.LeftWords, nodeXLControlLeft);
            ClusterAndDrawGraph(Learner.LocationInBipartiteGraph.RightWords, nodeXLControlRight);
        }

        private void ClusterAndDrawGraph(Learner.LocationInBipartiteGraph loc, NodeXLControl ctrl)
        {
            IGraph graph = learner.ReadLogicalGraph(loc);
            ICollection<Community> clusters = learner.GetClusters(graph);

            // One group will be created for each cluster.
            List<GroupInfo> groups = new List<GroupInfo>();

            foreach (Community cluster in clusters)
            {
                // Create a group.
                GroupInfo group = new GroupInfo();
                groups.Add(@group);

                // Populate the group with the cluster's vertices.
                foreach (IVertex vertex in cluster.Vertices)
                    @group.Vertices.AddLast(vertex);
            }

            // Store the group information as metadata on the graph.
            graph.SetValue(ReservedMetadataKeys.GroupInfo, groups.ToArray());

            // Assign the graph to the NodeXLControl.
            ctrl.Graph = graph;

            // Tell the layout class to use the groups.
            ctrl.Layout.LayoutStyle = LayoutStyle.UseGroups;

            // Tell the layout class how to lay out the groups.
            ctrl.Layout.BoxLayoutAlgorithm =
                BoxLayoutAlgorithm.Treemap;

            // Tell the NodeXLControl to lay out and draw the graph.
            ctrl.DrawGraph(true);
        }
    }
}
