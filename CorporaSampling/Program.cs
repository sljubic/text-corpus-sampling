using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GAF;
using GAF.Extensions;
using GAF.Operators;
using System.IO;
using System.ComponentModel;


namespace CorporaSampling
{
    class Program
    {
        static string baseDir = "C:\\_TEST\\";

        static void Main(string[] args)
        {

            /////////////////////////////////////////////////////////////////////////
            // Example: Find "the best" phrase set from the input corpus (SC), 
            //          using brute force (BF) approach FROM SCRATCH.
            //          "From scratch" means that the SC distribution, as well as 
            //          reduced N[]-gram RC are NOT known and NOT available.
            /////////////////////////////////////////////////////////////////////////
            /*
            CorpusTextSampler.BruteForceCorpusSampling_FromScratch(
                   baseDir + "OpenSubtitles2013.raw.hr.txt",   // Source corpus
                   200,                                        // Number of random trials
                   200,                                        // The size of the target phrase set
                   new[]{2, 3},                                // Word count of the selected phrases
                   baseDir + "CHARSET.txt",                    // Target charset
                   baseDir + "outRC.txt",                      // Reduced corpus (RC), output
                   baseDir + "outSET.txt",                     // Winning phrase set
                   baseDir + "outLETTERS_SC.csv",              // Letter distribution for the SC, computed
                   baseDir + "outLETTERS_PS.csv",              // Letter distribution for the TPS, computed
                   baseDir + "outDIGRAMS_SC.csv",              // Digram distribution for the SC, computed
                   baseDir + "outDIGRAMS_PS.csv",              // Digram distribution for the TPS, computed
                   baseDir + "outLOG.csv"                      // BF sampling log file
            );
            Console.WriteLine("BF finished. Phrase set is written to the output file.");
            */


            /////////////////////////////////////////////////////////////////////////
            // Example: Check that the best candidate found by BF approach
            //          has the same KLD value as reported in the BF process log.
            //          You should consider the log file produced by BF.
            /////////////////////////////////////////////////////////////////////////
            /*
            Distribution SC_Distribution = new Distribution(
                    baseDir + "outLETTERS_SC.csv", baseDir + "outDIGRAMS_SC.csv");
           
            Distribution best_Dist = new Distribution(
                    baseDir + "outSET.txt", baseDir + "CHARSET.txt", false, null, "");
           
            double kld1 = best_Dist.ComputeKLDivergence(SC_Distribution);
            Console.WriteLine(kld1);
            */


            /////////////////////////////////////////////////////////////////////////
            // Example: Find "the best" phrase set from the input corpus (SC), 
            //          using the GA approach.
            //          Assume that reduced corpus (RC) and digram distribution 
            //          of SC are already available.
            /////////////////////////////////////////////////////////////////////////
            /*
            HashSet<string> reducedCorpus = 
                    CorpusTextSampler.loadReducedDatasetFromFile(baseDir + "outRC.txt");
    
            Distribution SC_Distribution = new Distribution(
                    baseDir + "outLETTERS_SC.csv", baseDir + "outDIGRAMS_SC.csv");
            
            GA myGA = new GA(
                    baseDir + "CHARSET.txt",    // Target charset
                    SC_Distribution,            // Digram distribution of the SC
                    200,                        // The size of the target phrase set
                    reducedCorpus,              // Reduced corpus (RC)
                    500,                        // GA population size
                    true, 5,                    // GA elitism operator, percentage
                    0.8,                        // GA crossover probability
                    0.2, 20,                    // GA mutation probability, number of genes to be mutated                        
                    200,                        // Maximum number of generations
                    baseDir + "outSET_GA.txt",  // Winning phrase set
                    baseDir + "outLOG_GA.csv"   // GA sampling log file
            );
            
            myGA.Run();
            Console.WriteLine("GA finished. Phrase set is written to the output file.");
            */


            /////////////////////////////////////////////////////////////////////////
            // Example: Check that the best candidate found by GA approach
            //          has the same KLD value as reported in the GA process log.
            //          You should consider the log file produced by GA.
            /////////////////////////////////////////////////////////////////////////
            /*
            Distribution SC_Distribution = new Distribution(
                       baseDir + "outLETTERS_SC.csv", baseDir + "outDIGRAMS_SC.csv");
           
            Distribution best_Dist = new Distribution(
                   baseDir + "outSET_GA.txt", baseDir + "CHARSET.txt", false, null, "");
           
            double kld1 = best_Dist.ComputeKLDivergence(SC_Distribution);
            Console.WriteLine(kld1);
            */
            
            System.Console.ReadLine();
        }

    }
}
