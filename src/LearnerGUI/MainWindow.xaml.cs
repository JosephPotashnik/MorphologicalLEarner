using System.Collections.Generic;
using System.Windows;

namespace LearnerGUI
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            /*var learn = new Learner();

            learn.BuildBigramsandTrie();
            learn.BuildMorphologicalMatrix();
            learn.Search();
            
            InitializeComponentGraph(learn.NeighborGraph.LeftWordsNeighborhoods);
            */
            var LogicalGraph = new Dictionary<string, Dictionary<string, int>>();

            var innerDic = new Dictionary<string, int>();
            innerDic["sefi"] = 2;
            innerDic["daniel"] = 3;
            LogicalGraph["eli"] = innerDic;

            InitializeComponentGraph(LogicalGraph);

            gg_Area.GenerateGraph(true);
        }
    }
}