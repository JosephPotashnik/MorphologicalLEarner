namespace MorphologicalLearner
{
    public struct StringAlignmentData
    {
        private readonly TrieNode _father;
        private TrieNode _son;
        private string _difference;

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
        } //for now it will be just a string, later it will have to be something more complicated.

        public StringAlignmentData(TrieNode node)
        {
            _difference = "";
            _father = node;
            _son = null;

        }
    }
}
