/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.WpfGraphControl;
using MorphologicalLearner;
using ModifierKeys = System.Windows.Input.ModifierKeys;

namespace LearnerGUI
{
    internal class LearnerGUI : Application
    {


        private Learner learner;

        public static readonly RoutedUICommand LoadLeftGraphCommand = new RoutedUICommand("Left Graph...",
            "OpenFileCommand",
            typeof (LearnerGUI));

        public static readonly RoutedUICommand LoadRightGraphCommand = new RoutedUICommand("Right Graph...",
    "OpenFileCommand",
    typeof(LearnerGUI));

        public static readonly RoutedUICommand HomeViewCommand = new RoutedUICommand("Home view...", "HomeViewCommand",
            typeof (LearnerGUI));

        private readonly GraphViewer graphViewer1 = new GraphViewer();
        private readonly GraphViewer graphViewer2 = new GraphViewer();

        private readonly DockPanel graphViewerPanel1 = new DockPanel();
        private readonly DockPanel graphViewerPanel2 = new DockPanel();

        private readonly Grid mainGrid = new Grid();
        private readonly ToolBar toolBar = new ToolBar();
        private Window appWindow;
        private Window leftWindow;
        private Window rightWindow;

        private TextBox statusTextBox;
        public string[] Args { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
#if DEBUG
            DisplayGeometryGraph.SetShowFunctions();
#endif

            appWindow = new Window
            {
                Title = "LearnerGUI",
                Content = mainGrid,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowState = WindowState.Normal
            };

            Grid leftWindowGrid = new Grid();
            Grid rightWindowGrid = new Grid();


            leftWindow = new Window
            {
                Title = "LeftGraph",
                Content = leftWindowGrid,
                WindowStartupLocation = WindowStartupLocation.Manual,
                WindowState = WindowState.Normal
            };

             rightWindow = new Window
            {
                Title = "RightGraph",
                Content = rightWindowGrid,
                WindowStartupLocation = WindowStartupLocation.Manual,
                WindowState = WindowState.Normal
            };
            SetupToolbar();
            
            graphViewerPanel1.ClipToBounds = true;
            graphViewerPanel2.ClipToBounds = true;

            mainGrid.Children.Add(toolBar);
            toolBar.VerticalAlignment = VerticalAlignment.Top;
            graphViewer1.ObjectUnderMouseCursorChanged += graphViewer_ObjectUnderMouseCursorChanged;

            leftWindowGrid.Children.Add(graphViewerPanel1);
            graphViewer1.BindToPanel(graphViewerPanel1);

            rightWindowGrid.Children.Add(graphViewerPanel2);
            graphViewer2.BindToPanel(graphViewerPanel2);

            SetStatusBar();
            graphViewer1.MouseDown += WpfApplicationSample_MouseDown;
            graphViewer1.MouseUp += deleteSelected_Click;


            //CreateAndLayoutAndDisplayGraph(null, null);
            //graphViewer.MainPanel.MouseLeftButtonUp += TestApi;

            learner = new Learner("David Copperfield");

            appWindow.Show();
        }

        private void WpfApplicationSample_MouseDown(object sender, MsaglMouseEventArgs e)
        {
            statusTextBox.Text = "there was a click...";
        }

        private void deleteSelected_Click(object sender, EventArgs e)
        {
            var al = new ArrayList();
            foreach (IViewerObject ob in graphViewer1.Entities)
                if (ob.MarkedForDragging)
                    al.Add(ob);
            foreach (IViewerObject ob in al)
            {
                var edge = ob.DrawingObject as IViewerEdge;
                if (edge != null)
                    graphViewer1.RemoveEdge(edge, true);
                else
                {
                    var node = ob as IViewerNode;
                    if (node != null)
                        graphViewer1.RemoveNode(node, true);
                }
            }
        }
        private void SetStatusBar()
        {
            var statusBar = new StatusBar();
            statusTextBox = new TextBox {Text = "No object"};
            statusBar.Items.Add(statusTextBox);
            mainGrid.Children.Add(statusBar);
            statusBar.VerticalAlignment = VerticalAlignment.Bottom;
        }

        private void graphViewer_ObjectUnderMouseCursorChanged(object sender, ObjectUnderMouseCursorChangedEventArgs e)
        {
            var node = graphViewer1.ObjectUnderMouseCursor as IViewerNode;
            if (node != null)
            {
                var drawingNode = (Node) node.DrawingObject;
                statusTextBox.Text = drawingNode.Label.Text;
            }
            else
            {
                var edge = graphViewer1.ObjectUnderMouseCursor as IViewerEdge;
                if (edge != null)
                    statusTextBox.Text = ((Edge) edge.DrawingObject).SourceNode.Label.Text + "->" +
                                         ((Edge) edge.DrawingObject).TargetNode.Label.Text;
                else
                    statusTextBox.Text = "No object";
            }
        }

        private void SetupToolbar()
        {
            SetupCommands();
            DockPanel.SetDock(toolBar, Dock.Top);
            SetMainMenu();
            //edgeRangeSlider = CreateRangeSlider();
            // toolBar.Items.Add(edgeRangeSlider.Visual);
        }

        private void SetupCommands()
        {
            appWindow.CommandBindings.Add(new CommandBinding(LoadLeftGraphCommand, CreateAndLayoutAndDisplayGraph));
            appWindow.CommandBindings.Add(new CommandBinding(LoadRightGraphCommand, CreateAndLayoutAndDisplayGraph));

            appWindow.CommandBindings.Add(new CommandBinding(HomeViewCommand,
                (a, b) => graphViewer1.SetInitialTransform()));
        }

        private void ScaleNodeUpTest(object sender, ExecutedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void SetMainMenu()
        {
            var mainMenu = new Menu {IsMainMenu = true};
            toolBar.Items.Add(mainMenu);
            SetFileMenu(mainMenu);
            SetViewMenu(mainMenu);
        }

        private void SetViewMenu(Menu mainMenu)
        {
            var viewMenu = new MenuItem {Header = "_View"};
            var viewMenuItem = new MenuItem {Header = "_Home", Command = HomeViewCommand};
            viewMenu.Items.Add(viewMenuItem);
            mainMenu.Items.Add(viewMenu);
        }

        private void SetFileMenu(Menu mainMenu)
        {
            var fileMenu = new MenuItem {Header = "_File"};
            var openFileMenuItem1 = new MenuItem {Header = "_Left Graph", Command = LoadLeftGraphCommand};
            var openFileMenuItem2 = new MenuItem { Header = "_Right Graph", Command = LoadRightGraphCommand };

            fileMenu.Items.Add(openFileMenuItem1);
            fileMenu.Items.Add(openFileMenuItem2);

            mainMenu.Items.Add(fileMenu);
        }

        private void CreateAndLayoutAndDisplayGraph(object sender, ExecutedRoutedEventArgs ex)
        {
            Graph[] graphs = new Graph[2];
            graphs[0] = new Graph();
            graphs[1] = new Graph();
            learner.Learn();

            ReadLogicalGraph(graphs, 0, learner.NeighborGraph.LeftWordsNeighborhoods);
            ReadLogicalGraph(graphs, 1, learner.NeighborGraph.RightWordsNeighborhoods);

            graphViewer1.Graph = graphs[0];
            graphViewer2.Graph = graphs[1];

            rightWindow.Show();
            leftWindow.Show();
        }

        private void ReadLogicalGraph(Graph[] graphs, int i, Dictionary<string, Dictionary<string, int>> g)
        {
            try
            {
                graphs[i].LayoutAlgorithmSettings = new MdsLayoutSettings();
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
                        var e = graphs[i].AddEdge(word1, word2);
                        e.Attr.ArrowheadAtTarget = ArrowStyle.None;
                        e.Attr.LineWidth *= g[word1][word2]/3;
                        //the number of common neighbors determines the strength of the edge.
                    }
                }

                graphs[i].Attr.LayerDirection = LayerDirection.LR;

                graphs[i].LayoutAlgorithmSettings.EdgeRoutingSettings.RouteMultiEdgesAsBundles = true;
                //graph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.SplineBundling;
                //layout the graph and draw it
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [STAThread]
        private static void Main(string[] args)
        {
            new LearnerGUI {Args = args}.Run();
        }
    }
}