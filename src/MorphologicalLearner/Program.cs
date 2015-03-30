namespace MorphologicalLearner
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var learn = new Learner();

            learn.BuildBigramsandTrie();
            learn.BuildMorphologicalMatrix();
            learn.Search();
        }
    }
}