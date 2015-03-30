// This code is distributed under MIT license. Copyright (c) 2013 George Mamaladze
// See license.txt or http://opensource.org/licenses/mit-license.php

namespace MorphologicalLearner
{
    public class Trie : TrieNode
    {
        public Trie()
        {
            Add(""); //add the empty string in the root of the trie.
        }

        /*public IEnumerable<string> Retrieve(string query)
        {
            return Retrieve(query, 0);
        }*/

        public void Add(string key, string value)
        {
            Add(key, 0, value);
        }

        public void Add(string key)
        {
            Add(key, 0, key);
                // the key is both the path to the leaf node and also the value of the leaf node in a morphological learning task.
        }
    }
}