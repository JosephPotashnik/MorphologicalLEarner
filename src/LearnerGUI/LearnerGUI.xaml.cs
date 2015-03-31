﻿/*
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.WpfGraphControl;
using ModifierKeys = System.Windows.Input.ModifierKeys;

namespace LearnerGUI
{
    internal class LearnerGUI : Application
    {
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

            CreateAndLayoutAndDisplayGraph(null, null);
            //graphViewer.MainPanel.MouseLeftButtonUp += TestApi;
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

                //graph.LayoutAlgorithmSettings=new MdsLayoutSettings();

                graph.AddEdge("1", "2");
                graph.AddEdge("1", "3");
                var e = graph.AddEdge("4", "5");
                e.LabelText = "Some edge label";
                e.Attr.Color = Color.Red;
                e.Attr.LineWidth *= 2;

                graph.AddEdge("4", "6");
                e = graph.AddEdge("7", "8");
                e.Attr.LineWidth *= 2;
                e.Attr.Color = Color.Red;

                graph.AddEdge("7", "9");
                e = graph.AddEdge("5", "7");
                e.Attr.Color = Color.Red;
                e.Attr.LineWidth *= 2;

                graph.AddEdge("2", "7");
                graph.AddEdge("10", "11");
                graph.AddEdge("10", "12");
                graph.AddEdge("2", "10");
                graph.AddEdge("8", "10");
                graph.AddEdge("5", "10");
                graph.AddEdge("13", "14");
                graph.AddEdge("13", "15");
                graph.AddEdge("8", "13");
                graph.AddEdge("2", "13");
                graph.AddEdge("5", "13");
                graph.AddEdge("16", "17");
                graph.AddEdge("16", "18");
                graph.AddEdge("16", "18");
                graph.AddEdge("19", "20");
                graph.AddEdge("19", "21");
                graph.AddEdge("17", "19");
                graph.AddEdge("2", "19");
                graph.AddEdge("22", "23");

                e = graph.AddEdge("22", "24");
                e.Attr.Color = Color.Red;
                e.Attr.LineWidth *= 2;

                e = graph.AddEdge("8", "22");
                e.Attr.Color = Color.Red;
                e.Attr.LineWidth *= 2;

                graph.AddEdge("20", "22");
                graph.AddEdge("25", "26");
                graph.AddEdge("25", "27");
                graph.AddEdge("20", "25");
                graph.AddEdge("28", "29");
                graph.AddEdge("28", "30");
                graph.AddEdge("31", "32");
                graph.AddEdge("31", "33");
                graph.AddEdge("5", "31");
                graph.AddEdge("8", "31");
                graph.AddEdge("2", "31");
                graph.AddEdge("20", "31");
                graph.AddEdge("17", "31");
                graph.AddEdge("29", "31");
                graph.AddEdge("34", "35");
                graph.AddEdge("34", "36");
                graph.AddEdge("20", "34");
                graph.AddEdge("29", "34");
                graph.AddEdge("5", "34");
                graph.AddEdge("2", "34");
                graph.AddEdge("8", "34");
                graph.AddEdge("17", "34");
                graph.AddEdge("37", "38");
                graph.AddEdge("37", "39");
                graph.AddEdge("29", "37");
                graph.AddEdge("5", "37");
                graph.AddEdge("20", "37");
                graph.AddEdge("8", "37");
                graph.AddEdge("2", "37");
                graph.AddEdge("40", "41");
                graph.AddEdge("40", "42");
                graph.AddEdge("17", "40");
                graph.AddEdge("2", "40");
                graph.AddEdge("8", "40");
                graph.AddEdge("5", "40");
                graph.AddEdge("20", "40");
                graph.AddEdge("29", "40");
                graph.AddEdge("43", "44");
                graph.AddEdge("43", "45");
                graph.AddEdge("8", "43");
                graph.AddEdge("2", "43");
                graph.AddEdge("20", "43");
                graph.AddEdge("17", "43");
                graph.AddEdge("5", "43");
                graph.AddEdge("29", "43");
                graph.AddEdge("46", "47");
                graph.AddEdge("46", "48");
                graph.AddEdge("29", "46");
                graph.AddEdge("5", "46");
                graph.AddEdge("17", "46");
                graph.AddEdge("49", "50");
                graph.AddEdge("49", "51");
                graph.AddEdge("5", "49");
                graph.AddEdge("2", "49");
                graph.AddEdge("52", "53");
                graph.AddEdge("52", "54");
                graph.AddEdge("17", "52");
                graph.AddEdge("20", "52");
                graph.AddEdge("2", "52");
                graph.AddEdge("50", "52");
                graph.AddEdge("55", "56");
                graph.AddEdge("55", "57");
                graph.AddEdge("58", "59");
                graph.AddEdge("58", "60");
                graph.AddEdge("20", "58");
                graph.AddEdge("29", "58");
                graph.AddEdge("5", "58");
                graph.AddEdge("47", "58");

                var subgraph = new Subgraph("subgraph 1");
                graph.RootSubgraph.AddSubgraph(subgraph);
                subgraph.AddNode(graph.FindNode("47"));
                subgraph.AddNode(graph.FindNode("58"));

                graph.AddEdge(subgraph.Id, "55");

                var node = graph.FindNode("5");
                node.LabelText = "Label of node 5";
                node.Label.FontSize = 5;
                node.Label.FontName = "New Courier";
                node.Label.FontColor = Color.Blue;

                node = graph.FindNode("55");


                graph.Attr.LayerDirection = LayerDirection.LR;
                //   graph.LayoutAlgorithmSettings.EdgeRoutingSettings.RouteMultiEdgesAsBundles = true;
                //graph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.SplineBundling;
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