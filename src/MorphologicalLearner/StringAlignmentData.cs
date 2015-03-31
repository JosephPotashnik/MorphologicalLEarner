namespace MorphologicalLearner
{
    //reminder: struct (by c# rules) is copied by val, not by ref.
    public struct StringAlignmentData
    {
        private readonly TrieNode _father;
        private string _difference;
        private TrieNode _son;

        public StringAlignmentData(TrieNode node)
        {
            _difference = "";
            _father = node;
            _son = null;
        }

        public TrieNode Father
        {
            get { return _father; }
        }

        public TrieNode Son
        {
            get { return _son; }
            set { _son = value; }
        }

        public string Difference
        {
            get { return _difference; }
            set { _difference = value; }
        }
    }
}