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
            Vertex[] vertices = null;
            List<List<int>> communities = learner.ClusterWithLouvainMethod(loc);
            IGraph graph = learner.ReadLogicalGraph(loc, communities, out vertices);

            // One group will be created for each cluster.
            List<GroupInfo> groups = new List<GroupInfo>();

            foreach (List<int> cluster in communities)
            {
                if (cluster.Count > 1)
                {
                    // Create a group.
                    GroupInfo group = new GroupInfo();
                    groups.Add(@group);

                    //sefi all you need to do is to get the Ivertex from the node indeices.
                    // Populate the group with the cluster's vertices.
                    foreach (int nodeIndex in cluster)
                        @group.Vertices.AddLast(vertices[nodeIndex]);
                }
            }

            // Store the group information as metadata on the graph.
            graph.SetValue(ReservedMetadataKeys.GroupInfo, groups.ToArray());

            // Assign the graph to the NodeXLControl.
            ctrl.Graph = graph;

            // Tell the layout class to use the groups.
            ctrl.Layout.LayoutStyle = LayoutStyle.UseGroups;


            // Tell the layout class how to lay out the groups.
            ctrl.Layout.BoxLayoutAlgorithm =
                BoxLayoutAlgorithm.ForceDirected;

            // Tell the NodeXLControl to lay out and draw the graph.
            ctrl.DrawGraph(true);
        }
    }
}
