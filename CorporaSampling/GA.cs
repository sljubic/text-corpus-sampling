using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using GAF;
using GAF.Operators;

namespace CorporaSampling
{
    /// <summary>
    /// Genetic algorithm class
    /// </summary>

    class GA
    {
        private string charset;
        private Distribution corpusDistribution;
        private HashSet<int> usedGenes = new HashSet<int>();
        private HashSet<int> finalPhraseSet = new HashSet<int>();
        private HashSet<string> reducedCorpus;
        private GAF.GeneticAlgorithm myGA;
        private int stopGenCount;
        private string outputSolutionFile;
        private StreamWriter GAlog;

        private List<string> reducedCorpusList = new List<string>();


        /// <summary>
        /// Constructing genetic algorithm for text corpus sampling.
        /// </summary>
        /// <param name="charsetFilename">File(name) containing the target charset</param>
        /// <param name="corpusDistribution">SC digram distribution</param>
        /// <param name="phraseSetSize">Number of phrases in target phrase set</param>
        /// <param name="reducedCorpus">RC from which phrases will be selected</param>
        /// <param name="populationSize">GA population size</param>
        /// <param name="elitism">Flag denoting if elitism is used in a GA pipeline</param>
        /// <param name="elitismPercentage">GA elitism percentage</param>
        /// <param name="crossoverPercentage">GA crossover percentage</param>
        /// <param name="mutationProbability">GA mutation probability</param>
        /// <param name="genesAlteredByMutation">Number of genes in a chromosome altered by muatation</param>
        /// <param name="stopAtGenerationCount">GA termination criterion</param>
        /// <param name="outputSolutionFilename">File(name) containg the winning solution</param>
        /// <param name="GAlogFile">Log of the GA process</param>
        public GA(string charsetFilename, Distribution corpusDistribution, 
                    int phraseSetSize, HashSet<string> reducedCorpus,
                    int populationSize, 
                    bool elitism, 
                    int elitismPercentage, 
                    double crossoverPercentage, 
                    double mutationProbability, int genesAlteredByMutation, 
                    int stopAtGenerationCount,
                    string outputSolutionFilename, 
                    string GAlogFile)
        {
            this.charset = charsetFilename;
            this.corpusDistribution = corpusDistribution;
            this.stopGenCount = stopAtGenerationCount;
            this.reducedCorpus = reducedCorpus;
            reducedCorpusList = this.reducedCorpus.ToList();
            this.outputSolutionFile = outputSolutionFilename;
            this.GAlog = new StreamWriter(GAlogFile, false, Encoding.UTF8); 

            // Make an initial random population with <populationSize> chromosomes:
            var myPopulation = new Population(populationSize);

            var rand = new Random();
            for (int i = 0; i < populationSize; i++)
            {
                HashSet<int> tempList = new HashSet<int>();
                var chromosome = new Chromosome();

                // Fill a chromosome with <phraseSetSize> genes, without duplicates:
                while (tempList.Count < phraseSetSize)
                {
                    int randomPhraseIndex = rand.Next(reducedCorpus.Count);
                    if (!tempList.Contains(randomPhraseIndex))
                    {
                        tempList.Add(randomPhraseIndex);
                        chromosome.Genes.Add(new Gene(randomPhraseIndex));
                    }
                }

                tempList.Clear();
                myPopulation.Solutions.Add(chromosome);
            }

            // Which genes (sentences from the N[]-gram RC) are ONLY used in the current population?:
            resolveUsedGenesInCurrentPopulation(myPopulation);

            // Elite operator
            GAF.Operators.Elite myElite = null;
            if (elitism)
            {
                myElite = new Elite(elitismPercentage); 
            }

            // Crossover operator
            var myCrossover = new Crossover(crossoverPercentage, false, CrossoverType.DoublePoint);

            // Mutation operator (custom!) 
            var myMutate = new CustomMutateOperator(mutationProbability, genesAlteredByMutation, usedGenes, reducedCorpus.Count);

            // Create the GA instance:
            this.myGA = new GeneticAlgorithm(myPopulation, myFitnessFunction);

            // Event handlers:
            this.myGA.OnGenerationComplete += myGA_OnGenerationComplete;
            this.myGA.OnRunComplete += myGA_OnRunComplete;

            // Registering/adding operators to the GA:
            if ((elitism) && (myElite!=null))
            {
                this.myGA.Operators.Add(myElite);
            }

            this.myGA.Operators.Add(myCrossover);
            this.myGA.Operators.Add(myMutate);
        }



        /// <summary>
        /// GA run.
        /// </summary>
        public void Run()
        {
            try
            {
                myGA.Run(myTerminate);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }



        /// <summary>
        /// Criterion for stopping the GA.
        /// Here the generation count is utilized.
        /// </summary>
        /// <param name="population">GA population</param>
        /// <param name="currentGeneration">The number of current generation</param>
        /// <param name="currentEvaluation">The number of current evaluation</param>
        /// <returns></returns>
        public bool myTerminate(Population population, int currentGeneration, long currentEvaluation)
        {
            return currentGeneration > this.stopGenCount;
        }



        /// <summary>
        /// Event handler for the GA process completion.
        /// It reports a gene sequence for the chromosome with the best fitness value, 
        /// with corresponding phrases from the N[]-gram subset.
        /// In addition, the solution is written to a CSV file (index; phrase).
        /// </summary>
        private void myGA_OnRunComplete(object sender, GaEventArgs e)
        {
            var fittestChromosome = e.Population.GetTop(1)[0];
            foreach (var gene in fittestChromosome.Genes)
            {
                finalPhraseSet.Add((int)gene.ObjectValue);
            }
            writeSolutionToFile(fittestChromosome);
            
            Console.WriteLine("Max fitness: " + e.Population.MaximumFitness.ToString());
            Console.WriteLine("Min fitness: " + e.Population.MinimumFitness.ToString());
            Console.WriteLine("Fittest chromosome fitness: " + fittestChromosome.Fitness);

            var fittestKLD = getKLDivergenceForChromosome(fittestChromosome);
            Console.WriteLine("Fittest chromosome KLD: " + fittestKLD);

            GAlog.WriteLine(System.Math.Round(fittestChromosome.Fitness, 9) + ";" +
                            System.Math.Round(fittestKLD, 9));
            GAlog.Close();
        }



        /// <summary>
        /// Event handler for the evaluation completion of the single generation.
        /// It reports the current generation instance, best fitness value in the generation, and the corresponding KLD. 
        /// Also, the set of used genes in the whole population is recalculated.
        /// </summary>
        private void myGA_OnGenerationComplete(object sender, GaEventArgs e)
        {
            var fittestChromosome = e.Population.GetTop(1)[0];
            var fittestKLD = getKLDivergenceForChromosome(fittestChromosome);
            Console.WriteLine("Generation: {0}, Fitness: {1}, KLD: {2}", 
                    e.Generation, fittestChromosome.Fitness, fittestKLD);

            GAlog.WriteLine(System.Math.Round(fittestChromosome.Fitness, 9) + ";" +
                            System.Math.Round(fittestKLD, 9));

            resolveUsedGenesInCurrentPopulation(e.Population);
        }



        /// <summary>
        /// Calculates KL divergence (KLD) between input chromosome distribution and the SC distribution.
        /// </summary>
        /// <param name="chromosome">Input chromosome</param>
        /// <returns>KLD for a given chromosome</returns>
        private double getKLDivergenceForChromosome(Chromosome chromosome)
        {
            double KLD = -1.0;

            List<int> phraseIndexes = new List<int>();
            foreach (Gene iGene in chromosome)
            {
                phraseIndexes.Add((int)iGene.ObjectValue);
            }

            // Calculate distribution for a given chromosome:
            Distribution sampleDistribution = new Distribution(charset, reducedCorpusList, phraseIndexes);

            // Calculate KLD between sample/chromosome distribution and the SC distribution:
            KLD = sampleDistribution.ComputeKLDivergence(corpusDistribution);
            return KLD;
        }



        /// <summary>
        /// GA Fitness function. 
        /// It corresponds to a Kullback-Leibler Divergence (KLD) with a (1-KLD) relation.
        /// Namely, GA assumes value 1 as the "fittest", whereas optimum KLD is zero.
        /// </summary>
        /// <param name="chromosome">Input chromosome</param>
        /// <returns>Fitness value</returns>
        public double myFitnessFunction(Chromosome chromosome)
        {
            double kld = getKLDivergenceForChromosome(chromosome);
            return 1.0 - kld;
        }



        /// <summary>
        /// This method calculates only those phrases (genes) from the N[]-gram RC
        /// that are present in the current population.
        /// </summary>
        /// <param name="inPopulation">Current population</param>
        public void resolveUsedGenesInCurrentPopulation(Population inPopulation)
        {
            usedGenes.Clear();
            foreach (Chromosome iChrom in inPopulation.Solutions)
            {
                foreach (Gene iGene in iChrom)
                {
                    int geneValue = (int)iGene.ObjectValue;
                    if (!usedGenes.Contains(geneValue))
                    {
                        usedGenes.Add(geneValue);
                    }
                }
            }
        }



        /// <summary>
        /// Writes GA solution (fittest chromosome) to a file.
        /// </summary>
        /// <param name="chrome">Chromosome to be exported as a target phrase set</param>
        private void writeSolutionToFile(Chromosome chrome)
        {
            StreamWriter fileW = new StreamWriter(outputSolutionFile, false, Encoding.UTF8);
            foreach (Gene iGene in chrome.Genes)
            {
                int index = (int)iGene.ObjectValue;
                string line = reducedCorpus.ElementAt(index);
                fileW.WriteLine(line);
            }
            fileW.Close();
        }



    }
}
