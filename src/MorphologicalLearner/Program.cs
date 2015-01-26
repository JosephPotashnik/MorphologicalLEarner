namespace MorphologicalLearner
{
    class Program
    {


        static void Main(string[] args)
        {

            var learn = new Learner();

            learn.BuildTrie();
            learn.BuildBigrams();
            learn.BuildCandidates();

        }
    }
}
