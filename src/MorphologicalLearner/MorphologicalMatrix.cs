using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

namespace MorphologicalLearner
{
    public class MorphologicalMatrix
    {
        private const string SuffixDistributions = @"..\..\..\..\David Copperfield Suffix Distributions.txt";
        private string[] stemArray;
        private string[] suffixArray;
        private MorphologicalVector[] vectors;
        private Matrix<float> columnBasisMatrix;

        public MorphologicalMatrix(StemVector stems, SuffixVector suffixes)
        {
            var occMatrix = CreateOccurrencesMatrix(stems, suffixes);
            InitializeMorphologicalVectors(stems, occMatrix);
        }

        private Matrix<float> CreateOccurrencesMatrix(StemVector stems, SuffixVector suffixes)
        {
            stemArray = stems.GetAllStems();
            suffixArray = suffixes.GetAllSuffixes();

            var stemDic = new Dictionary<string, int>(); //get name of stem, returns index of the column in the matrix.
            var suffixDic = new Dictionary<string, int>(); //get name of suffix, return index of the row in the matrix.

            var i = 0;
            foreach (var s in stemArray)
                stemDic[s] = i++;

            i = 0;
            foreach (var s in suffixArray)
                suffixDic[s] = i++;

            var occurencesMatrix = Matrix<float>.Build.Dense(suffixArray.Count(), stemArray.Count());

            var ListOfStemsWithTheirSuffixes = stems.StemDic();
            foreach (var kvp in ListOfStemsWithTheirSuffixes)
            {
                foreach (var suffix in kvp.Value)
                {
                    //it is possible that the suffix has been omitted from the suffix vector because it was below 
                    //threshold of consideration. 
                    if (suffixDic.ContainsKey(suffix) /*&& suffix != Learner.StemSymbol*/)
                    {
                        occurencesMatrix[suffixDic[suffix], stemDic[kvp.Key]] = 1;
                    }
                }
            }

            return occurencesMatrix;
        }

        private void InitializeMorphologicalVectors(StemVector stems, Matrix<float> occurencesMatrix)
        {
            var Columns = occurencesMatrix.EnumerateColumns().ToArray();
            var ColumnBasis = Columns.Distinct().ToArray();
            columnBasisMatrix = Matrix<float>.Build.DenseOfColumnVectors(Columns.Distinct());

            vectors = new MorphologicalVector[ColumnBasis.Count()];
            
            for (var j = 0; j < ColumnBasis.Count(); ++j)
                vectors[j] = new MorphologicalVector();

            AddParticipatingSuffixesToMorphologicalVectors(ColumnBasis);  //unused; for debugging purposes (writing the suffixes' names)
            AddWordsToMorphologicalVectors(Columns, ColumnBasis, stems);
        }

        private void AddWordsToMorphologicalVectors(Vector<float>[] Columns, Vector<float>[] ColumnBasis, StemVector stems)
        {
            //sefi - repetition of code. please fix it.
            var i = 0;
            var suffixDic = new Dictionary<string, int>(); //get name of suffix, return index of the row in the matrix.
            foreach (var s in suffixArray)
                suffixDic[s] = i++;

            for (var k = 0; k < Columns.Count(); ++k)
            {
                for (var j = 0; j < ColumnBasis.Count(); ++j)
                {
                    if (!Columns[k].Equals(ColumnBasis[j])) continue;
                    
                    var derived = stems.GetAllDerivedForms(stemArray[k]);

                    foreach (var d in derived)
                    {
                        if (suffixDic.ContainsKey(d.Value))
                            //if the suffix has not been considered in the morphological matrix, don't add
                            vectors[j].AddWord(d.Key, suffixDic[d.Value]);
                    }
                    break;
                }
            }
        }

        private void AddParticipatingSuffixesToMorphologicalVectors(Vector<float>[] ColumnBasis)
        {
            for (var j = 0; j < ColumnBasis.Count(); ++j)
            {
                //get the suffix strings participating in the current column
                var SuffixNamesForColumn = ColumnBasis[j].Zip(suffixArray,
                    (f, s) => new {Number = f, SuffixName = s});

                var listOfSuffixes =
                    SuffixNamesForColumn.Where(c => c.Number > 0).Select(c => c.SuffixName);

                foreach (var str in listOfSuffixes)
                    vectors[j].AddSuffix(str);
            }
        }

        private float[] GetNormalizedFrequencies()
        {
            var numOfCat = vectors.Count();
            var normalizedFrequencies = new float[numOfCat];

            var minFreq = vectors[0].Count();
            var maxFreq = minFreq;

            //go over the categories and find the min/max frequencies.
            for (var i = 1; i < numOfCat; i++)
            {
                if (minFreq > vectors[i].Count())
                    minFreq = vectors[i].Count();
                if (maxFreq < vectors[i].Count())
                    maxFreq = vectors[i].Count();
            }

            //once obtained, normalize all frequencies relative to the distance between the min and max freq:
            var distance = maxFreq - minFreq;

            for (var i = 0; i < numOfCat; i++)
                normalizedFrequencies[i] = (vectors[i].Count() - minFreq)/(float) distance;

            return normalizedFrequencies;
        }

        private float[] GetNormalizedAmbiguities()
        {
            var rowSums = columnBasisMatrix.RowSums();
            var numOfRows = columnBasisMatrix.RowCount;

            float minAmbguityCount = 100; //arbitrary, guaranteed to get lower.
            float maxAmbiguityCount = 1;
            var NormalizedAmbiguityCounts = new float[numOfRows];

            for (var i = 0; i < numOfRows; i++)
            {
                if (minAmbguityCount > rowSums[i])
                    minAmbguityCount = rowSums[i];
                if (maxAmbiguityCount < rowSums[i])
                    maxAmbiguityCount = rowSums[i];
            }

            //once obtained, normalize all frequencies relative to the distance between the min and max ambiguity:
            var distance = maxAmbiguityCount - minAmbguityCount;

            //we want the most unambiguous cell to recieve the highest score, so invert. (1 - nomralized ambiguous cells)
            for (var i = 0; i < numOfRows; i++)
                NormalizedAmbiguityCounts[i] = 1 - (rowSums[i] - minAmbguityCount)/distance;

            return NormalizedAmbiguityCounts;
        }

        private string[] GetAllWordsWithGivenSuffixIndex(int index)
        {
            IEnumerable<string> str = Enumerable.Empty<string>();
            return vectors.Aggregate(str, (current, bucket) => current.Union(bucket.WordsOfSuffix(index))).ToArray();
        }

        public string[] FindSeed()
        {
            //the seed is a cell in the morphological matrix, A(i,j), which is factored from two components:
            //1. the heaviest bucket, i.e. the number of words having that inflection in this column (initial category)
            //cells that appear frequently in the corpus are better seeds.

            //note - the computation of the heaviest bucket is per word type, i.e. I count the number of the words in each bucket,
            //but I do not count here the tokens of each word.  (it is possible that there is a very light bucket with one word will have lots of tokens, I will not find it here).
            var normalizedFrequencies = GetNormalizedFrequencies();

            //2. the ambguity of the cell, i.e if the inflection does not appear in other columns (initial estimation of categories).
            //less ambiguous cells are better 
            var normalizedAmbiguities = GetNormalizedAmbiguities();

            //for the moment, assuming that the weights of the frequencies and of the ambiguities are the same.
            //we can test the learning for different weights.
            float frequencyWeight = 1;
            float AmbiguityWeight = 1;

            var weightedMatrix = Matrix<float>.Build.DenseOfMatrix(columnBasisMatrix);

            for (var i = 0; i < columnBasisMatrix.ColumnCount; i++)
                weightedMatrix.SetColumn(i,
                    columnBasisMatrix.Column(i).Multiply(frequencyWeight*normalizedFrequencies[i]));

            for (var i = 0; i < columnBasisMatrix.RowCount; i++)
                weightedMatrix.SetRow(i,
                    weightedMatrix.Row(i).Add(AmbiguityWeight*normalizedAmbiguities[i]));

            float MaxVal = 0;
            int maxRow = 0;
            int maxCol = 0;

            for (var k = 0; k < weightedMatrix.ColumnCount; k++)
            {
                for (var l = 0; l < weightedMatrix.RowCount; l++)
                {
                    if (weightedMatrix[l, k] > MaxVal)
                    {
                        maxRow = l;
                        maxCol = k;
                        MaxVal = weightedMatrix[l, k];
                    }
                }
            }
            //col 4 = {stem, ed, ing}
            //MaxCol = 0;

            //row 0 = stem, row 2 = ing, row 4 = ed.
            maxRow = 4; 
           //string suffixSeed = suffixArray[maxRow];
            
            //the entire column:
            //return vectors[maxCol].Words().ToArray();

            //the entire row:
            //return GetAllWordsWithGivenSuffixIndex(maxRow);

            //or the specific suffix in the column
            return vectors[maxCol].WordsOfSuffix(maxRow).ToArray();

        }

        public void PrintCategories()
        {
            var sb = new StringBuilder();
            sb.Append(String.Format("The groups for {0} word types (distinct words) of David Copperfield are",
                stemArray.Count()));
            sb.AppendLine();
            sb.AppendLine();

            foreach (var c in vectors)
            {
                sb.AppendFormat("Ending with {{{0}}} are:{1} {2} {3}",
                    string.Join(",", c.Suffixes().ToArray()),
                    Environment.NewLine,
                    string.Join(", ", c.Words().ToArray()),
                    Environment.NewLine);
                sb.AppendLine();
            }
            File.WriteAllText(SuffixDistributions, sb.ToString());
        }

        public IEnumerable<string> Words(int categoryIndex)
        {
            return vectors[categoryIndex].Words();
        }
    }
}