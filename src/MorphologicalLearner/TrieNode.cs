// This code is distributed under MIT license. Copyright (c) 2013 George Mamaladze
// See license.txt or http://opensource.org/licenses/mit-license.php
using System;
using System.Collections.Generic;
using System.Linq;

namespace MorphologicalLearner
{
    public class TrieNode 
    {
        private readonly Dictionary<char, TrieNode> m_Children;
        private readonly Queue<string> m_Values;

        protected TrieNode()
        {
            m_Children = new Dictionary<char, TrieNode>();
            m_Values = new Queue<string>();
        }

        public void Add(string key, int position, string value)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (EndOfString(position, key))
            {
                AddValue(value);
                return;
            }

            TrieNode child = GetOrCreateChild(key[position]);
            child.Add(key, position + 1, value);
        }

        private static bool EndOfString(int position, string text)
        {
            return position >= text.Length;
        }

        private TrieNode GetOrCreateChild(char key)
        {
            TrieNode result;
            if (!m_Children.TryGetValue(key, out result))
            {
                result = new TrieNode();
                m_Children.Add(key, result);
            }
            return result;
        }

        private TrieNode GetChildOrNull(string query, int position)
        {
            if (query == null) throw new ArgumentNullException("query");
            TrieNode childNode;
            return
                m_Children.TryGetValue(query[position], out childNode)
                    ? childNode
                    : null;
        }

        private void AddValue(string value)
        {
            m_Values.Enqueue(value);
        }
        public override string ToString()
        {
            return (m_Values.First());
        }

        public IEnumerable<StringAlignmentData> AmISuffix(StringAlignmentData data, char key)
        {
            //record the arc key transition on data alignment.
            data.Difference += key;

            //if I'm a suffix (i.e. the node has value content), return the data with the son pointer pointing to me
            if (m_Values.Count > 0)
            {
                data.Son = this;
                return Enumerable.Repeat(data, 1);
            }

            //otherwise, search sons
            return SearchSons(data);
        }

        public IEnumerable<StringAlignmentData> SearchSons(StringAlignmentData data)
        {
            IEnumerable<StringAlignmentData> list = Enumerable.Empty<StringAlignmentData>();

            foreach (KeyValuePair<char, TrieNode> kvp in m_Children)
                list = list.Concat(kvp.Value.AmISuffix(data, kvp.Key));

            return list;
        }

        public IEnumerable<StringAlignmentData> ExpandSuffixes()
        {
            StringAlignmentData d = new StringAlignmentData();
            d.Difference = "";
            d.Father = this;
            d.Son = null;

            return SearchSons(d);
        }

        /* UNUSED CODE - in working order.
        
         * //public IEnumerable<string> Values() { return m_Values;  }
        //private IEnumerable<TrieNode> Children() { return m_Children.Values; } //nodes of the children (i.e. the values of the dictionary}
         * 
        protected virtual IEnumerable<string> Retrieve(string query, int position)
        {
            return
                EndOfString(position, query)
                    ? ValuesClose() // ValuesDeep() // SEFI - changed from returning the values in the entire subtree of query to the closest values in the subtree
                    : SearchDeep(query, position);
        }

        protected virtual IEnumerable<string> SearchDeep(string query, int position)
        {
            TrieNode nextNode = GetChildOrNull(query, position);
            return nextNode != null
                       ? nextNode.Retrieve(query, position + 1)
                       : Enumerable.Empty<string>();
        }

        private IEnumerable<string> ValuesClose()
        {
            return Children().SelectMany(t => t.ValuesOrNull() ?? t.ValuesClose());
        }

        //if no value, return null. otherwise return the values sequence.
        private IEnumerable<string> ValuesOrNull()
        {
            foreach (var c in Values()) { return Values(); }
            return null;
        }
     
        private IEnumerable<string> ValuesDeep()
        {
            return
                Subtree()
                    .SelectMany(node => node.Values());
        }

        protected IEnumerable<TrieNode> Subtree()
        {

            return
                Enumerable.Repeat(this, 1)
                    .Concat(Children().SelectMany(child => child.Subtree()));
        }
             
      */
    }
}

