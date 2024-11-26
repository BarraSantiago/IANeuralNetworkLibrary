namespace NeuralNetworkLib.NeuralNetDirectory.NeuralNet
{
    public class Genome
    {
        private static readonly ThreadLocal<Random> random = new ThreadLocal<Random>(() => new Random());

        public float fitness;
        public float[] genome;

        public Genome(float[] genes)
        {
            genome = genes;
            fitness = 0;
        }

        public Genome(int genesCount)
        {
            genome = new float[genesCount];

            for (int j = 0; j < genesCount; j++)
                genome[j] = (float)(random.Value.NextDouble() * 2.0 - 1.0);

            fitness = 0;
        }

        public Genome()
        {
            fitness = 0;
        }
    }


    public class GeneticAlgorithm
    {
        private static readonly ThreadLocal<Random> random = new ThreadLocal<Random>(() => new Random());
        private readonly List<Genome> newPopulation = new List<Genome>();
        private readonly List<Genome> population = new List<Genome>();

        private readonly int eliteCount;
        private readonly float mutationChance;
        private readonly float mutationRate;

        private float totalFitness;

        public GeneticAlgorithm(int eliteCount, float mutationChance, float mutationRate)
        {
            this.eliteCount = eliteCount;
            this.mutationChance = mutationChance;
            this.mutationRate = mutationRate;
        }

        public Genome[] GetRandomGenomes(int count, int genesCount)
        {
            Genome[] genomes = new Genome[count];

            for (int i = 0; i < count; i++) genomes[i] = new Genome(genesCount);

            return genomes;
        }

        public List<Genome> Epoch(Genome[] oldGenomes, int totalCount)
        {
            totalFitness = 0;

            population.Clear();
            newPopulation.Clear();

            population.AddRange(oldGenomes);
            population.Sort(HandleComparison);

            foreach (Genome g in population) totalFitness += g.fitness;

            SelectElite();

            while (newPopulation.Count < (population.Count > 0 ? totalCount : 0)) Crossover();

            return newPopulation;
        }

        private void SelectElite()
        {
            for (int i = 0; i < eliteCount && newPopulation.Count < population.Count; i++)
                newPopulation.Add(population[i]);
        }

        private void Crossover()
        {
            const int maxRetries = 10;

            Genome mom = RouletteSelection();
            Genome dad = RouletteSelection();

            for (int i = 0; i < maxRetries; i++)
            {
                mom ??= RouletteSelection();
                dad ??= RouletteSelection();

                if (mom != null && dad != null)
                    break;
            }

            if (mom == null || dad == null) return;

            UniformCrossover(mom, dad, out Genome child1, out Genome child2);

            newPopulation.Add(child1);
            newPopulation.Add(child2);
        }

        private void SinglePivotCrossover(Genome mom, Genome dad, out Genome child1, out Genome child2)
        {
            child1 = new Genome();
            child2 = new Genome();

            child1.genome = new float[mom.genome.Length];
            child2.genome = new float[mom.genome.Length];

            int pivot = random.Value.Next(0, mom.genome.Length);

            for (int i = 0; i < pivot; i++)
            {
                child1.genome[i] = mom.genome[i];

                if (ShouldMutate())
                    child1.genome[i] += (float)(random.Value.NextDouble() * 2.0 - 1.0);

                child2.genome[i] = dad.genome[i];

                if (ShouldMutate())
                    child2.genome[i] += (float)(random.Value.NextDouble() * 2.0 - 1.0);
            }

            for (int i = pivot; i < mom.genome.Length; i++)
            {
                child2.genome[i] = mom.genome[i];

                if (ShouldMutate())
                    child2.genome[i] += (float)(random.Value.NextDouble() * 2.0 - 1.0);

                child1.genome[i] = dad.genome[i];

                if (ShouldMutate())
                    child1.genome[i] += (float)(random.Value.NextDouble() * 2.0 - 1.0);
            }
        }

        public void DoublePivotCrossover(Genome parent1, Genome parent2, out Genome child1, out Genome child2)
        {
            List<float> parent1Chromosome = parent1.genome.ToList();
            List<float> parent2Chromosome = parent2.genome.ToList();

            int chromosomeLength = parent1Chromosome.Count - 1;

            int locus = random.Value.Next(0, chromosomeLength);
            int length = random.Value.Next(0, (int)Math.Ceiling(chromosomeLength / 2.0));

            List<float> child1Chromosome = new List<float>();
            List<float> child2Chromosome = new List<float>();

            if (locus + length > chromosomeLength)
            {
                child1Chromosome.AddRange(parent2Chromosome.GetRange(0, (locus + length) % chromosomeLength));
                child1Chromosome.AddRange(parent1Chromosome.GetRange((locus + length) % chromosomeLength, locus));
                child1Chromosome.AddRange(parent2Chromosome.GetRange(locus, parent2Chromosome.Count - locus));

                child2Chromosome.AddRange(parent1Chromosome.GetRange(0, (locus + length) % chromosomeLength));
                child2Chromosome.AddRange(parent2Chromosome.GetRange((locus + length) % chromosomeLength, locus));
                child2Chromosome.AddRange(parent1Chromosome.GetRange(locus, parent1Chromosome.Count - locus));
            }
            else
            {
                child1Chromosome.AddRange(parent1Chromosome.GetRange(0, locus));
                child1Chromosome.AddRange(parent2Chromosome.GetRange(locus, length));
                child1Chromosome.AddRange(parent1Chromosome.GetRange(locus + length,
                    parent1Chromosome.Count - (locus + length)));

                child2Chromosome.AddRange(parent2Chromosome.GetRange(0, locus));
                child2Chromosome.AddRange(parent1Chromosome.GetRange(locus, length));
                child2Chromosome.AddRange(parent2Chromosome.GetRange(locus + length,
                    parent2Chromosome.Count - (locus + length)));
            }

            child1 = new Genome(child1Chromosome.GetRange(0, child1Chromosome.Count - 1).ToArray());
            child2 = new Genome(child2Chromosome.GetRange(0, child2Chromosome.Count - 1).ToArray());
        }

        private void UniformCrossover(Genome parent1, Genome parent2, out Genome child1, out Genome child2)
        {
            child1 = new Genome();
            child2 = new Genome();
            child1.genome = new float[parent1.genome.Length];
            child2.genome = new float[parent1.genome.Length];

            float selectionChance = 0.5f;

            for (int i = 0; i < parent1.genome.Length; i++)
            {
                if (random.Value.NextDouble() < selectionChance)
                {
                    child1.genome[i] = parent1.genome[i];
                    child2.genome[i] = parent2.genome[i];
                }
                else
                {
                    child1.genome[i] = parent2.genome[i];
                    child2.genome[i] = parent1.genome[i];
                }
            }
        }

        private bool ShouldMutate()
        {
            return random.Value.NextDouble() < mutationChance;
        }

        private static int HandleComparison(Genome x, Genome y)
        {
            return x.fitness > y.fitness ? 1 : x.fitness < y.fitness ? -1 : 0;
        }

        public Genome RouletteSelection()
        {
            float rnd = (float)(random.Value.NextDouble() * Math.Max(totalFitness, 0));

            float fitness = 0;

            for (int i = 0; i < population.Count; i++)
            {
                fitness += Math.Max(population[i].fitness, 0);
                if (fitness >= rnd)
                    return population[i];
            }

            return null;
        }
    }
}