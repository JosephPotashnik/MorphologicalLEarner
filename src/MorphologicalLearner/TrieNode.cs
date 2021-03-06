﻿// This code is distributed under MIT license. Copyright (c) 2013 George Mamaladze
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

            var child = GetOrCreateChild(key[position]);
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

        public TrieNode GetChildOrNull(string query, int position)
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
            return m_Children.SelectMany(kvp => kvp.Value.AmISuffix(data, kvp.Key));
        }

        //the function returns a list of all shortest words whose "this" node is their prefix (e.g. "intern", "interim", but not "interns" will be returned for "inter")
        public IEnumerable<StringAlignmentData> ExpandSuffixes()
        {
            var d = new StringAlignmentData(this);
            return SearchSons(d);
        }
    }
}