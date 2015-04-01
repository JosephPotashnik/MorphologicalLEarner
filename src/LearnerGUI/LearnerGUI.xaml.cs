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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.WpfGraphControl;
using MorphologicalLearner;
using ModifierKeys = System.Windows.Input.ModifierKeys;

namespace LearnerGUI
{
    internal class LearnerGUI : Application
    {

        private Learner learner;

        public static readonly RoutedUICommand LoadSampleGraphCommand = new RoutedUICommand("Open File...",
            "OpenFileCommand",
            typeof (LearnerGUI));

        public static readonly RoutedUICommand HomeViewCommand = new RoutedUICommand("Home view...", "HomeViewCommand",
            typeof (LearnerGUI));

        private readonly GraphViewer graphViewer = new GraphViewer();
        private readonly DockPanel graphViewerPanel = new DockPanel();
        private readonly Grid mainGrid = new Grid();
        private readonly ToolBar toolBar = new ToolBar();
        private Window appWindow;
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

            SetupToolbar();
            graphViewerPanel.ClipToBounds = true;
            mainGrid.Children.Add(toolBar);
            toolBar.VerticalAlignment = VerticalAlignment.Top;
            graphViewer.ObjectUnderMouseCursorChanged += graphViewer_ObjectUnderMouseCursorChanged;

            mainGrid.Children.Add(graphViewerPanel);
            graphViewer.BindToPanel(graphViewerPanel);

            SetStatusBar();
            graphViewer.MouseDown += WpfApplicationSample_MouseDown;

            //CreateAndLayoutAndDisplayGraph(null, null);
            //graphViewer.MainPanel.MouseLeftButtonUp += TestApi;


            learner = new Learner("David Copperfield");

            appWindow.Show();
        }

        private void WpfApplicationSample_MouseDown(object sender, MsaglMouseEventArgs e)
        {
            statusTextBox.Text = "there was a click...";
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
            var node = graphViewer.ObjectUnderMouseCursor as IViewerNode;
            if (node != null)
            {
                var drawingNode = (Node) node.DrawingObject;
                statusTextBox.Text = drawingNode.Label.Text;
            }
            else
            {
                var edge = graphViewer.ObjectUnderMouseCursor as IViewerEdge;
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
            appWindow.CommandBindings.Add(new CommandBinding(LoadSampleGraphCommand, CreateAndLayoutAndDisplayGraph));
            appWindow.CommandBindings.Add(new CommandBinding(HomeViewCommand,
                (a, b) => graphViewer.SetInitialTransform()));
            appWindow.InputBindings.Add(new InputBinding(LoadSampleGraphCommand,
                new KeyGesture(Key.L, ModifierKeys.Control)));
            appWindow.InputBindings.Add(new InputBinding(HomeViewCommand, new KeyGesture(Key.H, ModifierKeys.Control)));
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
            var openFileMenuItem = new MenuItem {Header = "_Load Sample Graph", Command = LoadSampleGraphCommand};
            fileMenu.Items.Add(openFileMenuItem);
            mainMenu.Items.Add(fileMenu);
        }

        private void CreateAndLayoutAndDisplayGraph(object sender, ExecutedRoutedEventArgs ex)
        {
            try
            {
                var graph = new Graph();

                learner.Learn();

                var g = learner.NeighborGraph.RightWordsNeighborhoods;
                HashSet<string> visitedNodes = new HashSet<string>();
                foreach (var node in g.Keys)
                {
                    visitedNodes.Add(node);
                    foreach (var outnode in g[node].Keys)
                    {
                            graph.AddEdge(node, outnode);
                    }
                }

                foreach (var node in g.Keys)
                {
                    visitedNodes.Add(node);
                    foreach (var outnode in g[node].Keys)
                    {
                        graph.AddEdge(node, outnode);
                    }
                }
                graph.Attr.LayerDirection = LayerDirection.LR;
                 graph.LayoutAlgorithmSettings.EdgeRoutingSettings.RouteMultiEdgesAsBundles = true;
                graph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.SplineBundling;
                //layout the graph and draw it
                graphViewer.Graph = graph;
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