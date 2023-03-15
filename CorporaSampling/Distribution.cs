using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CorporaSampling
{
    public class Distribution
    {
        Dictionary<string, long> dictionaryLetters;
        Dictionary<string, long> dictionaryDigrams;
        public long lettersTotalCount = 0;
        public long digramsTotalCount = 0;

        public enum TargetEntity
        {
            Letters,
            Digrams
        };


        /// <summary>
        /// Distribution constructor #1. 
        /// Loading letter/digram distributions from two dictionaries from memory.
        /// Letters distribution dictionary has to be the first parameter.
        /// </summary>
        /// <param name="letters">Dataset with letter occurences (probability)</param>
        /// <param name="digrams">Dataset with digram occurences (probability)</param>
        public Distribution(Dictionary<string, long> letters, Dictionary<string, long> digrams)
        {
            this.dictionaryLetters = new Dictionary<string,long>(letters);
            this.dictionaryDigrams = new Dictionary<string, long>(digrams);
            this.lettersTotalCount = dictionaryLetters.Sum(x => x.Value);
            this.digramsTotalCount = dictionaryDigrams.Sum(x => x.Value);
        }



        /// <summary>
        /// Distribution constructor #2.
        /// Loading letter/digram distributions from two corresponding CSV files.
        /// Letters distribution file(name) has to be the first parameter.
        /// </summary>
        /// <param name="lettersDistributionFile">Letters statistics input CSV file</param>
        /// <param name="digramsDistributionFile">Digrams statistics input CSV file</param>
        public Distribution(string lettersDistributionFile, string digramsDistributionFile)
        {
            string line;

            try
            {
                StreamReader lettersFile = new StreamReader(lettersDistributionFile, Encoding.UTF8);
                StreamReader digramsFile = new StreamReader(digramsDistributionFile, Encoding.UTF8);

                // Read header rows in CSV files:
                line = lettersFile.ReadLine();
                line = digramsFile.ReadLine();

                Dictionary<string, long> letters = new Dictionary<string, long>();
                Dictionary<string, long> digrams = new Dictionary<string, long>();

                // Read all other rows from letters-CSV file:
                while ((line = lettersFile.ReadLine()) != null)
                {
                    var values = line.Split(';');
                    letters.Add(values[0], long.Parse(values[1]));
                }
                lettersFile.Close();

                // Read all other rows from digrams-CSV file:
                while ((line = digramsFile.ReadLine()) != null)
                {
                    var values = line.Split(';');
                    digrams.Add(values[0], long.Parse(values[1]));
                }
                digramsFile.Close();

                this.dictionaryLetters = new Dictionary<string, long>(letters);
                this.dictionaryDigrams = new Dictionary<string, long>(digrams);
                this.lettersTotalCount = dictionaryLetters.Sum(x => x.Value);
                this.digramsTotalCount = dictionaryDigrams.Sum(x => x.Value);
            }
            catch (Exception)
            {
                Console.WriteLine("Letters/Digrams statistic files not found or wrongly formatted.");
                Console.ReadLine();
                Environment.Exit(0);       
            }
        }



        /// <summary>
        /// Distribution constructor #3.
        /// Computing letter/digram distributions from the corpus text file (SC), basing on the target charset.
        /// Considering possible huge corpus, this could be time-consuming.
        /// In addition, N[]-gram sentences can be extracted in a separate corpus subset (RC).
        /// </summary>
        /// <param name="corpusFilename">
        ///     Source corpus (SC) filename
        /// </param>
        /// <param name="charsetFilename">
        ///     Filename containing target charset (UTF-8)
        /// </param>
        /// <param name="makeReduction">
        ///     If true, reduced N[]-gram corpus (RC) will be extracted in a separate file
        /// </param>
        /// <param name="nWords">
        ///     Number of words in phrases that will be extracted to RC (array, e.g. {4, 5}).
        /// </param>
        /// <param name="reducedCorpusFilename">
        ///     RC output filename; will not be used if [makeReduction] is false
        /// </param>
        public Distribution(
                string corpusFilename,
                string charsetFilename,
                bool makeReduction,
                int[] nWords,
                string reducedCorpusFilename)
        {
            var reducedCorpusPhrases = new HashSet<string>();

            // Initializing dictionaries; based on the target charset:
            List<Dictionary<string, long>> emptyDict = makeDictionary(charsetFilename);
            Dictionary<string, long> dictLetters = new Dictionary<string, long>(emptyDict[0]);
            Dictionary<string, long> dictDigrams = new Dictionary<string, long>(emptyDict[1]);

            string line;
            string lowercaseLine;

            // Uncomment for console output:
            /*
            System.Console.WriteLine("Starting calculation of the SC statistics.");
            if (makeReduction)
                System.Console.WriteLine("Additionally,  " + nWord + "-words phrases will be extracted in a separate file.");
            
            DateTime dateCalculationStart = DateTime.Now;
            */

            StreamReader charsetFile = new StreamReader(charsetFilename, Encoding.UTF8);
            string charsetString = charsetFile.ReadLine();
            charsetFile.Close();

            StreamReader corpusFile = new StreamReader(corpusFilename, Encoding.UTF8);

            while ((line = corpusFile.ReadLine()) != null)
            {
                // All to lowercase: 
                lowercaseLine = line.ToLower();
                bool allRegularCharsInLine = true;

                // Update letters and digrams statistics (of the SC) on the current line basis:
                for (int i = 0; i < lowercaseLine.Length; i++)
                {
                    // adding to letters dictionary
                    string key = lowercaseLine[i].ToString();
                    if (dictLetters.ContainsKey(key))
                    {
                        dictLetters[key]++;
                    }
                    else
                    {
                        allRegularCharsInLine = false;
                    }

                    // adding to digrams dictionary
                    if (i <= lowercaseLine.Length - 2)
                    {
                        key += lowercaseLine[i + 1];
                        if (dictDigrams.ContainsKey(key))
                        {
                            dictDigrams[key]++;
                        }
                    }
                }

                // If corpus reduction has to be performed, choose only phrases that contain exactly <nWords> words.
                // While doing so, do not consider duplicates.
                if (makeReduction)
                {
                    if (allRegularCharsInLine &&
                        nWords.Contains(CorpusTextSampler.numberOfWordsInLine(lowercaseLine)) &&
                        (!reducedCorpusPhrases.Contains(lowercaseLine)))
                    {
                        reducedCorpusPhrases.Add(lowercaseLine);
                    }
                }

            }
            corpusFile.Close();

            //  If corpus reduction has been performed, write RC dataset (N-grams) to a file:
            if (makeReduction)
            {
                StreamWriter reducedCorpusFile = new StreamWriter(reducedCorpusFilename, false, Encoding.UTF8);
                foreach (string phrase in reducedCorpusPhrases)
                {
                    reducedCorpusFile.WriteLine(phrase);
                }
                reducedCorpusFile.Close();
            }

            // Uncomment for console output:
            /*
            DateTime dateCalculationEnd = DateTime.Now;
            string logText = "SC statistics calculation ";
            if (makeReduction) logText += "and N-gram reduced dataset RC ";
            logText += "successfully performed in: " + (dateCalculationEnd - dateCalculationStart).TotalSeconds.ToString() + " seconds.";
            System.Console.WriteLine(logText);
            System.Console.WriteLine();
            */

            this.dictionaryLetters = new Dictionary<string, long>(dictLetters);
            this.dictionaryDigrams = new Dictionary<string, long>(dictDigrams);
            this.lettersTotalCount = dictionaryLetters.Sum(x => x.Value);
            this.digramsTotalCount = dictionaryDigrams.Sum(x => x.Value);
        }



        /// <summary>
        /// Distribution constructor #4.
        /// Computing letter/digram distributions for a Phrase Set Candidate (PSC) that origins from reduced N[]-gram dataset.
        /// Phrase set candidate is represented by a set of indexes that point to phrases from the RC.
        /// This method is intended to be used by a genetic algorithm.
        /// </summary>
        /// <param name="charsetFilename">
        ///     Filename containing target charset (UTF-8)
        /// </param>
        /// <param name="reducedSetList">
        ///     RC containing only N[]-word sentences; should be already lowercased and interpunction-cleared</param>
        /// <param name="indexSet">
        ///     Index set (integer indices targeting RC), forming the Phrase set candidate</param>
        public Distribution(string charsetFilename, List<string> reducedSetList, List<int> indexSet)
        {
            List<Dictionary<string, long>> emptyDict = makeDictionary(charsetFilename);
            Dictionary<string, long> dictLetters = emptyDict[0];
            Dictionary<string, long> dictDigrams = emptyDict[1];

            string currentLine;

            for (int i = 0; i < indexSet.Count; i++)
            {
                // Get a specific sentence from the reduced N[]-gram dataset RC:
                currentLine = reducedSetList[indexSet[i]];

                // RC should be previously lowercased and interpunction-cleared.
                // Update letter/digram statistics, based on given line:
                for (int j = 0; j < currentLine.Length; j++)
                {
                    string key = currentLine[j].ToString();
                    if (dictLetters.ContainsKey(key))
                    {
                        dictLetters[key]++;
                    }

                    if (j <= currentLine.Length - 2)
                    {
                        key += currentLine[j + 1];
                        if (dictDigrams.ContainsKey(key))
                        {
                            dictDigrams[key]++;
                        }
                    }
                }
            }

            this.dictionaryLetters = new Dictionary<string, long>(dictLetters);
            this.dictionaryDigrams = new Dictionary<string, long>(dictDigrams);
            this.lettersTotalCount = dictionaryLetters.Sum(x => x.Value);
            this.digramsTotalCount = dictionaryDigrams.Sum(x => x.Value);
        }



        /// <summary>
        /// Calculating Pearson product-moment correlation coefficient between this distribution (p)
        /// and the input distribution (q). Distributions with same number of parameters are assumed.
        /// </summary>
        /// <param name="q">
        ///     Input dataset (usually SC) distribution
        /// </param>
        /// <param name="entity">
        ///     Entity for which correlation is calculated (Letters or Digrams)
        /// </param>
        /// <returns>
        ///     Pearson product-moment correlation between two distributions (of letters/digrams)
        /// </returns>
        public double ComputeCorrelation(Distribution q, TargetEntity entity)
        {
            Dictionary<string, long> entitiesP = null;
            Dictionary<string, long> entitiesQ = null;
            double avgP = 0.0;
            double avgQ = 0.0;
            double correlation = 0.0;

            if (entity == TargetEntity.Letters)
            {
                entitiesP = this.dictionaryLetters;
                entitiesQ = q.dictionaryLetters;
                avgP = (double)this.lettersTotalCount / (double)entitiesP.Count();
                avgQ = (double)q.lettersTotalCount / (double)entitiesQ.Count();
            }
            else if (entity == TargetEntity.Digrams)
            {
                entitiesP = this.dictionaryDigrams;
                entitiesQ = q.dictionaryDigrams;
                avgP = (double)this.digramsTotalCount / (double)entitiesP.Count();
                avgQ = (double)q.digramsTotalCount / (double)entitiesQ.Count();
            }

            double sumUp = 0.0;
            double sumDownP = 0.0;
            double sumDownQ = 0.0;

            // Calculating Pearson product-moment correlation coefficient:
            if ((entitiesP!=null) && (entitiesQ!=null))
            {
                foreach (KeyValuePair<string, long> pair in entitiesP)
                {
                    sumUp += ((pair.Value - avgP) * (entitiesQ[pair.Key] - avgQ));
                    sumDownP += Math.Pow((pair.Value - avgP), 2.0);
                    sumDownQ += Math.Pow((entitiesQ[pair.Key] - avgQ), 2.0);
                }
                correlation = sumUp / Math.Sqrt(sumDownP * sumDownQ);
            }

            return correlation;
        }



        /// <summary>
        /// Calculating digram-based relative entropy (Kullback-Leibler Divergence, KLD) between 
        /// the phrase set instance (this, p) and the source corpus (input distribution, q). 
        /// </summary>
        /// <param name="q">
        ///     Input (SC) distribution
        /// </param>
        /// <returns>
        ///     Kullback-Leibler Divergence between the phrase set instance and the SC
        /// </returns>
        public double ComputeKLDivergence(Distribution q)
        {
            double divergence = 0.0;
            foreach (KeyValuePair<string, long> pair in this.dictionaryDigrams)
            {
                double probabilityP = (double)pair.Value / (double)this.digramsTotalCount;
                double probabilityQ = (double)q.dictionaryDigrams[pair.Key] / (double)q.digramsTotalCount;
                if ((probabilityQ != 0.0) && (probabilityP != 0.0))
                    divergence += probabilityP * Math.Log(probabilityP / probabilityQ, 2.0);
            }
            return divergence;
        }



        /// <summary>
        /// Writing distribution to a CSV file
        /// </summary>
        /// <param name="entity">
        ///     Entity for which distribution is written to a file (Letters/Digrams)
        /// </param>
        /// <param name="filename"
        ///     >Distribution output filename
        /// </param>
        public void WriteDistributionToCSV(TargetEntity entity, string filename)
        {
            Dictionary<string, long> entities = new Dictionary<string, long>();
            string ent = "";
            long sumTotal = 0;

            if (entity == TargetEntity.Letters)
            {
                entities = this.dictionaryLetters;
                ent = "Letter";
                sumTotal = this.lettersTotalCount;
            }
            else if (entity == TargetEntity.Digrams)
            {
                entities = this.dictionaryDigrams;
                ent = "Digram";
                sumTotal = this.digramsTotalCount;
            }

            StreamWriter distributionOutputFile = new StreamWriter(filename, false, Encoding.UTF8);
            // Write header row:
            distributionOutputFile.WriteLine(ent + ";" + ent + " occurences;" + ent + " probability");

            // Write all other lines (letter/digram; letter/digram occurences; letter/digram probability)
            foreach (KeyValuePair<string, long> pair in entities)
            {
                double probability = (double)pair.Value / (double)sumTotal;
                String rowLine = pair.Key + ";" + pair.Value + ";" + Math.Round(probability, 9);
                distributionOutputFile.WriteLine(rowLine);
            }

            distributionOutputFile.Close();
        }



        /// <summary>
        /// Initializing letters and digrams dictionaries.
        /// Letters and digrams containing entities other than those in target charset will NOT be calculated in statistics.
        /// </summary>
        /// <param name="filename">
        ///     Target charset filename, containing single line with all considered characters, encoded in UTF-8.
        /// </param>
        /// <returns>
        ///     List of initialized dictionaries (letters, digrams)
        /// </returns>
        public static List<Dictionary<string, long>> makeDictionary(string charsetFilename)
        {
            char[] charset = null;

            // Reading charset form the input filename line:
            try
            {
                StreamReader charsetFile = new StreamReader(charsetFilename, Encoding.UTF8);
                string charsetLine = charsetFile.ReadLine();
                charset = charsetLine.ToCharArray();
                charsetFile.Close();
            }
            catch (Exception)
            {
                Console.WriteLine("Charset file not found or wrongly formatted.");
                Console.ReadLine();
                Environment.Exit(0);
            }

            Dictionary<string, long> initDigrams = new Dictionary<string, long>();
            Dictionary<string, long> initLetters = new Dictionary<string, long>();

            // Initializing dictionaries:
            for (int i = 0; i < charset.Length; i++)
            {
                string key = charset[i].ToString();
                initLetters.Add(key, 0);

                for (int j = 0; j < charset.Length; j++)
                {
                    initDigrams.Add(key + charset[j], 0);
                }
            }

            List<Dictionary<string, long>> returnList = new List<Dictionary<string, long>>();
            returnList.Add(initLetters);
            returnList.Add(initDigrams);

            return returnList;
        }



    }
}
