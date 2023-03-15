using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GAF;
using GAF.Operators;

namespace CorporaSampling
{
    // Our custom mutation operator derives from built in SWAP MUTATE operator.
    
    // From the official API documentation:
    // The Swap Mutate operator, when enabled, traverses EACH gene in the population and, 
    // based on the probability swaps one gene in the chromosome with another. 
    // The aim of this operator is to provide mutation without changing any gene values. 

    // Our mutation operator needs to SUBSTITUTE genes in such a way that introduces 
    // completely new genes that are unavailable in the current population. 
    // In order for new replecements to become present in the chromosome, 
    // some existing genes, of course, have to be excluded from it.


    public class CustomMutateOperator : SwapMutate
    {
        private int numberOfPhrasesInReducedCorpus;
        private HashSet<int> usedGenesOnly = new HashSet<int>();
        Random rand = new Random();
        private int changingGenesCount;


        /// <summary>
        /// Custom mutate operator constructor
        /// </summary>
        /// <param name="mutationProbability">The probability of altering the chromosome</param>
        /// <param name="genesToChange">The number of genes that will be altered by mutation</param>
        /// <param name="usedGenes">All genes already present in the whole population</param>
        /// <param name="numPhrasesInReducedSet">Number of phrases in the RC</param>
        public CustomMutateOperator(
                double mutationProbability, 
                int genesToChange, 
                HashSet<int> usedGenes, 
                int numPhrasesInReducedSet) : base(mutationProbability)
        {
            this.usedGenesOnly = usedGenes;
            this.numberOfPhrasesInReducedCorpus = numPhrasesInReducedSet;
            this.changingGenesCount = genesToChange;
            this.MutationProbability = mutationProbability;
            this.Enabled = true;
        }



        /// <summary>
        /// Overriden Mutate method provides our own mutation implementation.
        /// Based on the [mutationProbability], only certain chromosomes will be mutated in the population.
        /// Mutation is represented by replacing target genes with completely new genes/sentences
        /// from the RC (that doesn't exist in the current population anyhow).
        /// </summary>
        /// <param name="chromosome">Target chromosome</param>
        /// <param name="mutationProbability">The probability of mutation</param>
        protected override void Mutate(Chromosome chromosome, double mutationProbability)
        {
            // Perform custom mutation only if target chromosome isn't in the elite set
            if (!chromosome.IsElite)
            {
                // Based on the mutation probability, resolve if this chromosome should mutate:
                bool mutationNeeded = false;
                int randomNo = rand.Next(100);
                int upperBound = (int)(100 * this.MutationProbability);
                if ((randomNo >= 0) && (randomNo <= upperBound)) mutationNeeded = true;

                // Perform mutation, if needed
                if (mutationNeeded)
                {
                    // Find distinct random genes that will be replaced: 
                    List<int> changeIndexes = new List<int>();
                    while (changeIndexes.Count < changingGenesCount)
                    {
                        int candidate = rand.Next(chromosome.Genes.Count());
                        if (!changeIndexes.Contains(candidate))
                        {
                            changeIndexes.Add(candidate);
                        }
                    }

                    // Find NEW sentences/genes/indexes that doesn't exist in the population yet
                    // (without duplicates):
                    List<int> changeValues = new List<int>();
                    while (changeValues.Count < changingGenesCount)
                    {
                        int candidate = rand.Next(numberOfPhrasesInReducedCorpus);
                        if ((!usedGenesOnly.Contains(candidate)) && (!changeValues.Contains(candidate)))
                        {
                            changeValues.Add(candidate);
                        }
                    }

                    // Perform mutation by replacing genes:
                    for (int i = 0; i < changingGenesCount; i++)
                    {
                        chromosome.Genes[changeIndexes[i]] = new Gene(changeValues[i]);
                    }
                }
            }
        }



    }

}
