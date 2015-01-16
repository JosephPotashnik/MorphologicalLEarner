namespace MorphologicalLearner
{
    class Program
    {


        static void Main(string[] args)
        {

            Learner learn = new Learner();

            learn.BuildTrie();
            learn.BuildCandidates();

        }
    }
}
