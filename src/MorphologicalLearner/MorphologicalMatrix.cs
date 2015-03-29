using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Complex;
using QuickGraph;

namespace MorphologicalLearner
{
    public class MorphologicalMatrix
    {
        private const string SuffixDistributions = @"..\..\..\..\David Copperfield Suffix Distributions.txt";
        
        private readonly string[] stemArray;
        private readonly string[] suffixArray;

        private Matrix<float> matrix;
        private Matrix<float> columnBasisMatrix;

        private MorphologicalBucket[] buckets;

        public Matrix<float> Matrix {
            get { return matrix; }
        }

        private string StemAt(int index)
        {
            return stemArray[index];
        }

        private string SuffixAt(int index)
        {
            return suffixArray[index];
        }

        public MorphologicalMatrix(StemVector stems, SuffixVector suffixes)
        {    
            stemArray = stems.GetAllStems();
            suffixArray = suffixes.GetAllSuffixes();

            var stemDic = new Dictionary<string, int>(); //get name of stem, returns index of the column in the matrix.
            var suffixDic = new Dictionary<string, int>(); //get name of suffix, return index of the row in the matrix.

            int i = 0;
            foreach (var s in stemArray)
                stemDic[s] = i++;

            i = 0;
            foreach (var s in suffixArray)
                suffixDic[s] = i++;

            matrix = Matrix<float>.Build.Dense(suffixArray.Count(), stemArray.Count());
            //string[,] inflectedArray = new string[suffixArray.Count(), stemArray.Count()];

            var ListOfStemsWithTheirSuffixes = stems.StemDic();
            foreach (var kvp in ListOfStemsWithTheirSuffixes)
            {
                foreach (var suffix in kvp.Value)
                {
                    //it is possible that the suffix has been omitted from the suffix vector because it was below 
                    //threshold of consideration.
                    if (suffixDic.ContainsKey(suffix))
                    {
                        matrix[suffixDic[suffix], stemDic[kvp.Key]] = 1;
                        //inflectedArray[suffixDic[suffix], stemDic[kvp.Key]] = "";
                    }
                }
            }
        }

        public MorphologicalBucket[] InitializeMorphologicalBuckets(StemVector stems)
        {
            Vector<float>[] Columns = matrix.EnumerateColumns().ToArray();
            var ColumnBasis = Columns.Distinct().ToArray();
            columnBasisMatrix = Matrix<float>.Build.DenseOfColumnVectors(Columns.Distinct());

            //we will take later each bucket to be the first approximation of a syntactic category.
            //this is a safe assumption as the number of  different attested combinations will be far greater
            //then the mumber of syntactic categories. if there are elements belonging to different syntactic categories, 
            //they will have to be recognized as such and serparted in a later stage.
            buckets = new MorphologicalBucket[ColumnBasis.Count()];
          
            //create a bucket that represents a certain morphological pattern (i.e. a column in the morphological matrix)
            //and push inside all the words that match that pattern.

            AddParticipatingSuffixesToBuckets(ColumnBasis);

            AddWordsToBuckets(Columns, ColumnBasis, stems);

            return buckets;
        }

        private void AddWordsToBuckets(Vector<float>[] Columns, Vector<float>[] ColumnBasis, StemVector stems)
        {
            for (int k = 0; k < Columns.Count(); ++k)
            {
                for (int j = 0; j < ColumnBasis.Count(); ++j)
                {
                    if (!Columns[k].Equals(ColumnBasis[j])) continue;
                    buckets[j].Add(stemArray[k]);

                    List<string> derived = stems.GetAllDerivedForms(stemArray[k]);
                    foreach (var d in derived)
                        buckets[j].Add(d);
                    break;
                }
            }
        }

        private void AddParticipatingSuffixesToBuckets(Vector<float>[] ColumnBasis)
        {
            for (int j = 0; j < ColumnBasis.Count(); ++j)
            {
                buckets[j] = new MorphologicalBucket();

                //get the suffix strings participating in the current column
                var SuffixNamesForColumn = ColumnBasis[j].Zip(suffixArray,
                    (f, s) => new {Number = f, SuffixName = s});

                IEnumerable<string> listOfSuffixes =
                    SuffixNamesForColumn.Where(c => c.Number > 0).Select(c => c.SuffixName);

                foreach (var str in listOfSuffixes)
                    buckets[j].AddSuffix(str);
            }
        }

        private float[] GetNormalizedFrequencies()
        {
            int numOfCat = buckets.Count();
            float[] normalizedFrequencies = new float[numOfCat];

            int minFreq = buckets[0].Count();
            int maxFreq = minFreq;

            //go over the categories and find the min/max frequencies.
            for (int i = 1; i < numOfCat; i++)
            {
                if (minFreq > buckets[i].Count())
                    minFreq = buckets[i].Count();
                if (maxFreq < buckets[i].Count())
                    maxFreq = buckets[i].Count();
            }

            //once obtained, normalize all frequencies relative to the distance between the min and max freq:
            int distance = maxFreq - minFreq;

            for (int i = 0; i < numOfCat; i++)
                normalizedFrequencies[i] = (float)(buckets[i].Count() - minFreq) / (float)distance;

            return normalizedFrequencies;
        }

        private float[] GetNormalizedAmbiguities()
        {

            Vector<float> rowSums = columnBasisMatrix.RowSums();
            int numOfRows = columnBasisMatrix.RowCount;

            float minAmbguityCount = 100; //arbitrary, guaranteed to get lower.
            float maxAmbiguityCount = 1;
            float[] NormalizedAmbiguityCounts = new float[numOfRows];

            for (int i = 0; i < numOfRows; i++)
            {
                if (minAmbguityCount > rowSums[i])
                    minAmbguityCount = rowSums[i];
                if (maxAmbiguityCount < rowSums[i])
                    maxAmbiguityCount = rowSums[i];      
            }

            //once obtained, normalize all frequencies relative to the distance between the min and max ambiguity:
            float distance = maxAmbiguityCount - minAmbguityCount;

            //we want the most unambiguous cell to recieve the highest score, so invert. (1 - nomralized ambiguous cells)
            for (int i = 0; i < numOfRows; i++)
                NormalizedAmbiguityCounts[i] = 1 - (rowSums[i] - minAmbguityCount) / distance;

            return NormalizedAmbiguityCounts;
        }

        public int FindSeed()
        {
            
            //the seed is a cell in the morphological matrix, A(i,j), which is factored from two components:
            //1. the heaviest bucket, i.e. the number of words having that inflection in this column (initial category)
            //cells that appear frequently in the corpus are better seeds.

            //note - the computation of the heaviest bucket is per word type, i.e. I count the number of the words in each bucket,
            //but I do not count here the tokens of each word.  (it is possible that there is a very light bucket with one word will have lots of tokens, I will not find it here).
            float[] normalizedFrequencies = GetNormalizedFrequencies();

            //2. the ambguity of the cell, i.e if the inflection does not appear in other columns (initial estimation of categories).
            //less ambiguous cells are better 
            float[] normalizedAmbiguities = GetNormalizedAmbiguities();


            //for the moment, assuming that the weights of the frequencies and of the ambiguities are the same.
            //we can test the learning for different weights.
            float frequencyWeight = 1;
            float AmbiguityWeight = 1;

            var weightedMatrix = Matrix<float>.Build.DenseOfMatrix(columnBasisMatrix);

            for (int i = 0; i < columnBasisMatrix.ColumnCount; i++)
                weightedMatrix.SetColumn(i,
                    columnBasisMatrix.Column(i).Multiply(frequencyWeight * normalizedFrequencies[i]));

            for (int i = 0; i < columnBasisMatrix.RowCount; i++)
                weightedMatrix.SetRow(i,
                weightedMatrix.Row(i).Add(AmbiguityWeight * normalizedAmbiguities[i]));

            int maxRow = 0, maxCol = 0;
            float MaxVal = 0;

            for (int k = 0; k < weightedMatrix.ColumnCount; k++)
            {
                for (int l = 0; l < weightedMatrix.RowCount; l++)
                {
                    if (weightedMatrix[l, k] > MaxVal)
                    {
                        maxRow = l;
                        maxCol = k;
                        MaxVal = weightedMatrix[l, k];
                    }
                }
            }

                            //string suffixSeed = suffixArray[maxRow];
            return maxCol;  //MorphologicalBucket seed = buckets[maxCol];


        }

        public void PrintCategories()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(String.Format("The groups for {0} word types (distinct words) of David Copperfield are", stemArray.Count()));
            sb.AppendLine();
            sb.AppendLine();

            foreach (var c in buckets)
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
            return buckets[categoryIndex].Words();
        }

        
    }
}
