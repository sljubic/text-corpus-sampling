using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CorporaSampling
{
    static public class CorpusTextSampler
    {

        // String used for logging purposes
        static string logHeader =
                    "Number of letters in the source corpus;" +
                    "Number of digrams in the source corpus;" +
                    "Number of sentences in the N-gram corpus subset;" +
                    "Trial;" +
                    "Number of sentences in the instance Phrase set;" +
                    "Number of letters in the instance Phrase set;" +
                    "Number of digrams in the instance Phrase set;" +
                    "Average number of letters per sentence in the instance Phrase set;" +
                    "Correlation coefficeint (letter-based) between the SC and the instance Phrase set;" +
                    "Correlation coefficeint (digram-based) between the SC and the instance Phrase set;" +
                    "Relative entropy between the SC and the instance Phrase set";


        /// <summary>
        /// Using "brute force" (BF) approach with limited number of trials.
        /// This method generates the most representative phrase set from the available source corpus (SC).
        /// This method is used when both the SC distribution and the reduced corpus (RC) are known and available.
        /// </summary>
        /// <param name="corpusLetterDistributionFilename">
        ///     Input filename containing distribution of letters in the source corpus
        /// </param>
        /// <param name="corpusDigramDistributionFilename">
        ///     Input filename containing distribution of digrams in the source corpus
        /// </param>
        /// <param name="reducedCorpusFilename">
        ///     Input filename containing RC limited to only N-words sentences
        /// </param>
        /// <param name="numTrials">
        ///     Number of trials for randomizing and evaluating phrase set instance
        /// </param>
        /// <param name="numPhrases">
        ///     Number of phrases to be included in the target phrase set
        /// </param>
        /// <param name="charsetFilename">
        ///     Filename containing target charset (UTF-8)
        /// </param>
        /// <param name="outPhraseSetFilename">
        ///     Target phrase set filename (final solution)
        /// </param>
        /// <param name="outLettersInPhraseSetDistributionFilename">
        ///     Output CSV filename containing distribution of letters in the generated phrase set
        /// </param> 
        /// <param name="outDigramsInPhraseSetDistributionFilename">
        ///     Output CSV filename containing distribution of digrams in the generated phrase set
        /// </param> 
        /// <param name="outTrialLogFilename">
        ///     Output log filename containing related parameters for each randomization-and-evaluation try
        /// </param>
        static public void BruteForceCorpusSampling_withDistributionAlreadyKnown(
                                        string corpusLetterDistributionFilename,
                                        string corpusDigramDistributionFilename,
                                        string reducedCorpusFilename,
                                        int numTrials,
                                        int numPhrases,
                                        string charsetFilename,
                                        string outPhraseSetFilename,
                                        string outLettersInPhraseSetDistributionFilename,
                                        string outDigramsInPhraseSetDistributionFilename,
                                        string outTrialLogFilename)
        {
            System.Console.WriteLine("===>>>START<<<===");

            // Read SC statistics from file:
            Distribution corpusDistribution = new Distribution(corpusLetterDistributionFilename, corpusDigramDistributionFilename);

            // Read RC sentences from file:
            var corpusPhrasesReduced = new HashSet<string>();
            corpusPhrasesReduced = loadReducedDatasetFromFile(reducedCorpusFilename);

            // Perform BF competition among <numTrials> random datasets; 
            // retreive the distribution for the best candidate:
            Distribution bestPhraseSetDistribution =
                CompeteRandomDatasets(charsetFilename, corpusDistribution, corpusPhrasesReduced,
                                        numPhrases, numTrials, 
                                        outTrialLogFilename, outPhraseSetFilename);

            // Write letter/digram distributions from the winning phrase set to corresponding files:
            bestPhraseSetDistribution.WriteDistributionToCSV(
                Distribution.TargetEntity.Letters, outLettersInPhraseSetDistributionFilename);
            bestPhraseSetDistribution.WriteDistributionToCSV(
                Distribution.TargetEntity.Digrams, outDigramsInPhraseSetDistributionFilename);

            System.Console.WriteLine("===>>>END<<<===");
        }



        /// <summary>
        /// Using "brute force" (BF) approach with limited number of trials.
        /// This method generates the most representative phrase set from the available SC.
        /// Here, it is assumed that no calculations are made upfront, so time-consuming initial 
        /// distribution calculation and corpus reduction will be performed. 
        /// In addition, method generates 4 files: letter/digram distributions for both 
        /// the SC and the winning (the best solution) phrase set.
        /// </summary>
        /// <param name="corpusFilename">
        ///     SC filename
        /// </param>
        /// <param name="numTrials">
        ///     Number of trials for randomizing and evaluating phrase set instance
        /// </param>
        /// <param name="numPhrases">
        ///     Number of phrases to be included in the target phrase set
        /// </param>
        /// <param name="wordsInPhrases">
        ///     Number of words in phrases that will be extracted to RC (array, e.g. {4, 5})
        /// </param>
        /// <param name="charsetFilename">
        ///     Filename containing target charset (UTF-8)
        /// </param>
        /// <param name="outReducedCorpusFilename">
        ///     Filename containing RC limited to only N-words sentences
        /// </param> 
        /// <param name="outPhraseSetFilename">
        ///     Target phrase set filename (final solution)
        /// </param>
        /// <param name="outLettersInCorpusDistributionFilename">
        ///     Output CSV filename containing distribution of letters in the SC
        /// </param>
        /// <param name="outLettersInPhraseSetDistributionFilename">
        ///     Output CSV filename containing distribution of letters in the generated phrase set
        /// </param>
        /// <param name="outDigramsInCorpusDistributionFilename">
        ///     Output CSV filename containing distribution of digrams in the SC
        /// </param>
        /// <param name="outDigramsInPhraseSetDistributionFilename">
        ///     Output CSV filename containing distribution of digrams in the generated phrase set
        /// </param>
        /// <param name="outTrialLogFilename">
        ///     Output log filename containing related parameters for each randomization-and-evaluation try
        /// </param>
        static public void BruteForceCorpusSampling_FromScratch(
                            string corpusFilename,
                            int numTrials,
                            int numPhrases,
                            int[] wordsInPhrases,
                            string charsetFilename,
                            string outReducedCorpusFilename,
                            string outPhraseSetFilename,
                            string outLettersInCorpusDistributionFilename,
                            string outLettersInPhraseSetDistributionFilename,
                            string outDigramsInCorpusDistributionFilename,
                            string outDigramsInPhraseSetDistributionFilename,
                            string outTrialLogFilename)
        {
            System.Console.WriteLine("===>>>START<<<===");

            // Calculate SC statistics and obtain reduced N-gram dataset:
            Distribution corpusDistribution = 
                new Distribution(corpusFilename, charsetFilename, true, wordsInPhrases, outReducedCorpusFilename);

            // Load RC from file into RAM:
            var corpusPhrasesReduced = new HashSet<string>();
            corpusPhrasesReduced = loadReducedDatasetFromFile(outReducedCorpusFilename);

            // Perform BF competition among <numTrials> random datasets; 
            // retreive the distribution for the best candidate:
            Distribution bestPhraseSetDistribution = 
                CompeteRandomDatasets(charsetFilename, corpusDistribution, corpusPhrasesReduced, 
                                        numPhrases, numTrials, 
                                        outTrialLogFilename, outPhraseSetFilename);

            // Write letter/digram distributions from the winning phrase set to corresponding files:
            corpusDistribution.WriteDistributionToCSV(
                Distribution.TargetEntity.Letters, outLettersInCorpusDistributionFilename);
            corpusDistribution.WriteDistributionToCSV(
                Distribution.TargetEntity.Digrams, outDigramsInCorpusDistributionFilename);
            bestPhraseSetDistribution.WriteDistributionToCSV(
                Distribution.TargetEntity.Letters, outLettersInPhraseSetDistributionFilename);
            bestPhraseSetDistribution.WriteDistributionToCSV(
                Distribution.TargetEntity.Digrams, outDigramsInPhraseSetDistributionFilename);

            System.Console.WriteLine("===>>>END<<<===");
        }



        /// <summary>
        /// "Brute force" (BF) generating target Phrase set by making competition among N randomly generated datasets.
        /// Random datasets are derived from the RC that has previously been lowercased and/or interpunction-cleared.
        /// The best candidate shows minimum KL divergence (relative entropy) with the SC digram-based distribution.
        /// Single trial parameters are logged into corresponding output log file.
        /// Additionally, the best candidate (target Phrase set) is written to a file.
        /// </summary>
        /// <param name="charsetFilename">
        ///     Filename containing target charset (UTF-8)
        /// </param>
        /// <param name="corpusDistribution">
        ///     SC digram-based distribution (previously computed)
        /// </param>
        /// <param name="reducedCorpus">
        ///     Lowercased and interpunction-cleared RC
        /// </param>
        /// <param name="phraseNum">
        ///     Number of phrases in the target Phrase set (and all competing datasets)
        /// </param>
        /// <param name="trials">
        ///     Number of trials (number of competing random datasets)
        /// </param>
        /// <param name="logFilename">
        ///     Output log CSV filename
        /// </param>
        /// <param name="phraseSetFilename">
        ///     Output filename containing target Phrase set (best candidate)
        /// </param>
        /// <returns>
        ///     Digram-based distribution for the winning (the best) phrase set
        /// </returns>
        public static Distribution CompeteRandomDatasets(
                string charsetFilename, 
                Distribution corpusDistribution, 
                HashSet<string> reducedCorpus,
                int phraseNum, 
                int trials, 
                string logFilename, 
                string phraseSetFilename)
        {
            // Initializing dictionaries
            List<Dictionary<string, long>> emptyDict = Distribution.makeDictionary(charsetFilename);
            Dictionary<string, long> dictLetters = new Dictionary<string, long>(emptyDict[0]);
            Dictionary<string, long> dictDigrams = new Dictionary<string, long>(emptyDict[1]);

            // Prepare and write header row in log file:
            StreamWriter fileLog = new StreamWriter(logFilename, false, Encoding.UTF8);
            fileLog.WriteLine(logHeader);
            
            var rand = new Random();
            double KLD_best = double.MaxValue;
            HashSet<string> bestPhraseSet = null;
            Distribution tempPhraseSetDistribution = null;

            // Indexing => List; Searching => Hashset
            List<string> reducedPhraseList = reducedCorpus.ToList();

            // Run trials by randomizing and evaluating dataset instances:
            for (int trial = 1; trial <= trials; trial++)
            {
                // Generate sample phrase set WITHOUT duplicate phrases:
                HashSet<string> phraseSet = new HashSet<string>();
                while (phraseSet.Count < phraseNum)
                {
                    string phraseInstance = reducedPhraseList[rand.Next(reducedCorpus.Count)];
                    if (!phraseSet.Contains(phraseInstance))
                    {
                        phraseSet.Add(phraseInstance);
                    }
                }

                // Compute letter/digram distributions for this dataset instance:
                foreach (string setLine in phraseSet)
                {
                    // RC should already be lowercased and interpunction-cleared
                    for (int i = 0; i < setLine.Length; i++)
                    {
                        // adding to letters dictionary
                        string key = setLine[i].ToString();
                        if (dictLetters.ContainsKey(key))
                        {
                            dictLetters[key]++;
                        }
                        
                        // adding to digrams dictionary
                        if (i <= setLine.Length - 2)
                        {
                            key += setLine[i + 1];
                            if (dictDigrams.ContainsKey(key))
                            {
                                dictDigrams[key]++;
                            }
                        }
                    } 
                }

                // Register letter/digram distributions of the phrase set instance:
                Distribution phraseSetDistribution = new Distribution(dictLetters, dictDigrams);

                // Clear utility dictionaries:
                foreach (var key in dictLetters.Keys.ToList())
                {
                    dictLetters[key] = 0;
                }
                foreach (var key in dictDigrams.Keys.ToList())
                {
                    dictDigrams[key] = 0;
                }

                // Compute KLD between the digram models:
                double relativeEntropy = phraseSetDistribution.ComputeKLDivergence(corpusDistribution);

                // Compute letters/digrams correlations:
                double lettersCorrel =
                    phraseSetDistribution.ComputeCorrelation(corpusDistribution, Distribution.TargetEntity.Letters);
                double digramsCorrel =
                    phraseSetDistribution.ComputeCorrelation(corpusDistribution, Distribution.TargetEntity.Digrams);

                // Output to console:
                /*
                System.Console.WriteLine("Trial " + trial.ToString() + " -- KLD: " + relativeEntropy.ToString() +
                                         " -- COR(let): " + lettersCorrel.ToString() + " -- COR(dig): " + digramsCorrel.ToString());
                */

                // Update best phrase set:
                if (relativeEntropy < KLD_best)
                {
                    KLD_best = relativeEntropy;
                    bestPhraseSet = phraseSet;
                    tempPhraseSetDistribution = phraseSetDistribution;
                }

                // Log parameters from the current trial:
                fileLog.WriteLine(
                            corpusDistribution.lettersTotalCount + ";" + 
                            corpusDistribution.digramsTotalCount + ";" +
                            reducedCorpus.Count() + ";" + 
                            trial + ";" + 
                            phraseSet.Count() + ";" +
                            phraseSetDistribution.lettersTotalCount + ";" + 
                            phraseSetDistribution.digramsTotalCount + ";" +
                            Math.Round(((double)phraseSetDistribution.lettersTotalCount / (double)phraseSet.Count()), 2) + ";" +
                            lettersCorrel.ToString() + ";" + 
                            digramsCorrel.ToString() + ";" + 
                            relativeEntropy.ToString());
            }
            fileLog.Close();

            // Write the winning phrase set to a file:
            File.WriteAllLines(phraseSetFilename, bestPhraseSet);

            return tempPhraseSetDistribution;
        }



        /// <summary>
        /// Method that reduces the available SC and returns RC containing only N[]-words sentences.
        /// RC is written to a filename.
        /// No statistics/distribution calculation is performed whatsoever.
        /// </summary>
        /// <param name="corpusFilename">
        ///     Source corpus filename
        /// </param>
        /// <param name="charsetFilename">
        ///     Target charset filename
        /// </param>
        /// <param name="nWords">
        ///     Possible numbers of words in phrases that will be extracted to RC
        /// </param>
        /// <param name="reducedFilename">
        ///     Filename containing RC limited to only N[]-words sentences
        /// </param>
        static public void SourceCorpus_ReduceOnly(
                string corpusFilename, 
                string charsetFilename, 
                int[] nWords, 
                string reducedFilename)
        {
            var reducedCorpusPhrases = new HashSet<string>();
            string line;
            string lowercaseLine;

            // Uncomment for console output:
            /*
            System.Console.WriteLine("Starting phrase extraction from the SC, without any distribution calculation.");
            DateTime dateExtractionStart = DateTime.Now;
            */

            StreamReader charsetFile = new StreamReader(charsetFilename, Encoding.UTF8);
            string charsetString = charsetFile.ReadLine();
            charsetFile.Close();

            StreamReader corpusFile = new StreamReader(corpusFilename, Encoding.UTF8);
            while ((line = corpusFile.ReadLine()) != null)
            {
                // all to lowercase: 
                lowercaseLine = line.ToLower();

                // clear basic interpunction:
                // [ comment or adapt, according to the context ]
                lowercaseLine = clearInterpunction(lowercaseLine);

                // For the RC, choose only those phrases that contain exactly <nWord> words.
                // In addition, remove duplicates.
                if (nWords.Contains(numberOfWordsInLine(lowercaseLine)) &&
                    lowercaseLine.All(c => charsetString.Contains(c)) &&
                    (!reducedCorpusPhrases.Contains(lowercaseLine)))
                {
                    reducedCorpusPhrases.Add(lowercaseLine);
                }
            }
            corpusFile.Close();

            // Output RC dataset (N[]-grams) to a file:
            StreamWriter reducedCorpusFile = new StreamWriter(reducedFilename, false, Encoding.UTF8);
            foreach (string phrase in reducedCorpusPhrases)
            {
                reducedCorpusFile.WriteLine(phrase);
            }
            reducedCorpusFile.Close();

            // Uncomment for console output:
            /*
            DateTime dateExtractionEnd = DateTime.Now;
            string extractionTime = (dateExtractionEnd - dateExtractionStart).TotalSeconds.ToString();
            System.Console.WriteLine("SC successfully extracted N[]-grams to a file in: " + extractionTime + " seconds.");
            System.Console.WriteLine();
            */ 
        }



        /// <summary>
        /// Loading RC, containing a subset of the SC, from a txt file.
        /// </summary>
        /// <param name="reducedDataSetFile">
        ///     Input filename containing RC with N-gram sentences
        /// </param>
        /// <returns>Reduced corpus (RC), as hashset</returns>
        public static HashSet<string> loadReducedDatasetFromFile(string reducedDataSetFile)
        {
            HashSet<string> reducedSet = new HashSet<string>();
            string line;

            try
            {
                StreamReader reducedFile = new StreamReader(reducedDataSetFile, Encoding.UTF8);
                while ((line = reducedFile.ReadLine()) != null)
                {
                    reducedSet.Add(line);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Reduced N[]-gram dataset file not found or wrongly formatted.");
                Console.ReadLine();
                Environment.Exit(0);
            }
            return reducedSet;
        }

   

        /// ----- utility methods -----



        /// <summary>
        /// Clearing basic interpunction from the respective lowercase line, 
        /// including multiple spaces, and leading spaces.
        /// </summary>
        /// <param name="myLine">
        ///     Current line that is being cleared from basic interpunction.
        /// </param>
        /// <returns>Line without basic interpunction characters.</returns>
        public static string clearInterpunction(string myLine)
        {
            // Uncomment for 'manual' clearing... 
            /*
            myLine = myLine.Replace(", ", " ");
            myLine = myLine.Replace(" ,", " ");

            myLine = myLine.Replace(". ", " ");
            myLine = myLine.Replace(" .", " ");

            myLine = myLine.Replace("; ", " ");
            myLine = myLine.Replace(" ;", " ");

            myLine = myLine.Replace(": ", " ");
            myLine = myLine.Replace(" :", " ");

            myLine = myLine.Replace("\" ", " ");
            myLine = myLine.Replace(" \"", " ");

            myLine = myLine.Replace("' ", " ");
            myLine = myLine.Replace(" '", " ");

            myLine = myLine.Replace("- ", " ");
            myLine = myLine.Replace(" -", " ");

            myLine = myLine.Replace("?", " ");
            myLine = myLine.Replace("!", " ");

            myLine = myLine.Replace("  ", " ");

            myLine = myLine.TrimStart(new char[] { ' ' });
            */

            char[] arr = myLine.ToCharArray();
            arr = Array.FindAll<char>(arr, (c => (char.IsLetter(c) || char.IsWhiteSpace(c) /*|| (c=='\'')*/)));
            myLine = new string(arr);
            myLine = string.Join(" ", myLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

            return myLine;
        }



        /// <summary>
        /// Calculating number of words in the respective line/phrase 
        /// </summary>
        /// <param name="myLine">Input line</param>
        /// <returns>Number of words in the input line</returns>
        public static int numberOfWordsInLine(string myLine)
        {
            char[] seperator = new char[] { ' ' };
            int numberOfWords = myLine.Split(seperator, StringSplitOptions.RemoveEmptyEntries).Length;
            return numberOfWords;
        }



        /// <summary>
        /// Calculating number of characters in the respective line/phrase.
        /// Blanks/spaces are not counted. 
        /// </summary>
        /// <param name="myLine">Input line</param>
        /// <returns>Number of characters in the input line</returns>
        public static int numberOfCharactersInLine(string myLine)
        {
            int n = 0;
            foreach (char c in myLine)
            {
                if (c != ' ') n++;
            }
            return n;
        }



        /// <summary>
        /// Calculating number of characters per word in the Phrase set
        /// </summary>
        /// <param name="phraseSet">Target phrase set</param>
        /// <returns>Number of characters per word</returns>
        public static double charsPerWord(string phraseSet)
        {
            StreamReader corpusFile = new StreamReader(phraseSet, Encoding.UTF8);
            string line = "";
            long chars = 0;
            long words = 0;

            while ((line = corpusFile.ReadLine()) != null)
            {
                int w = numberOfWordsInLine(line);
                int c = numberOfCharactersInLine(line);
                chars += c;
                words += w;
            }
            corpusFile.Close();
            
            return (double)chars / (double)words;
        }



    }
}
