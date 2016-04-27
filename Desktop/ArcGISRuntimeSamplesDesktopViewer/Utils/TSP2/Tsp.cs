//////////////////////////////////////////////////////////////////////////////////////////////////
// File Name: Tsp.cs
//      Date: 06/01/2006
// Copyright (c) 2006 Michael LaLena. All rights reserved.  (www.lalena.com)
// Permission to use, copy, modify, and distribute this Program and its documentation,
//  if any, for any purpose and without fee is hereby granted, provided that:
//   (i) you not charge any fee for the Program, and the Program not be incorporated
//       by you in any software or code for which compensation is expected or received;
//   (ii) the copyright notice listed above appears in all copies;
//   (iii) both the copyright notice and this Agreement appear in all supporting documentation; and
//   (iv) the name of Michael LaLena or lalena.com not be used in advertising or publicity
//          pertaining to distribution of the Program without specific, written prior permission. 
///////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;
using System.Threading.Tasks;
using Tsp;

namespace ArcGISRuntime.Samples.DesktopViewer.Utils.TSP2
{
    /// <summary>
    /// This class performs the Travelling Salesman Problem algorithm.
    /// </summary>
    class Tsp
    {
        /// <summary>
        /// Delegate used to raise an event when a new best tour is found.
        /// </summary>
        /// <param name="sender">Object that generated this event.</param>
        /// <param name="e">Event arguments. Contains information about the best tour.</param>
        public delegate void NewBestTourEventHandler(Object sender, TspEventArgs e);

        /// <summary>
        /// Event fired when a new best tour is found.
        /// </summary>
        public event NewBestTourEventHandler FoundNewBestTour;


        public delegate void NewCalcStartedEventHandler(Object sender, TspCalcEventArgs e);
        public event NewCalcStartedEventHandler newCalcStarted;

        /// <summary>
        /// Random number generator object.
        /// We allow the GUI to set the seed for the random number generator to assist in debugging.
        /// This allows errors to be easily reproduced.
        /// </summary>
        Random rand;

        /// <summary>
        /// The list of TSPVertices. This is only used to calculate the distances between the TSPVertices.
        /// </summary>
        TSPVertices tspVertexList;

        /// <summary>
        /// The complete list of all the tours.
        /// </summary>
        Population population;

        /// <summary>
        /// Private copy of a flag that will stop the TSP from calculating any more generations.
        /// </summary>
        private bool halt = false;
        /// <summary>
        /// The GUI sets this flag to true to stop the TSP algorithm and allow the Begin() function to return.
        /// </summary>
        public bool Halt
        {
            get
            {
                return halt;
            }
            set
            {
                halt = value;
            }
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public Tsp()
        {
        }

        /// <summary>
        /// Starts the TSP algorithm.
        /// To stop before all generations are calculated, set <see cref="Halt"/> to true.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="populationSize">Number of random tours to create before starting the algorithm.</param>
        /// <param name="maxGenerations">Number of times to perform the crossover operation before stopping.</param>
        /// <param name="groupSize">Number of tours to examine in each generation. Top 2 are chosen as the parent tours whose children replace the worst 2 tours in the group.</param>
        /// <param name="mutation">Odds that a child tour will be mutated..</param>
        /// <param name="seed">Seed for the random number generator.</param>
        /// <param name="chanceToUseCloseCity">The odds (out of 100) that a city that is known to be close will be used in any given link.</param>
        /// <param name="tspVertexListparameter"></param>
        /// <param name="cityList">List of TSPVertices in the tour.</param>
        public void Begin(CancellationToken token, int populationSize, int maxGenerations, int groupSize, int mutation, int seed, int chanceToUseCloseCity, TSPVertices tspVertexListparameter)
        {
            rand = new Random(seed);

            this.tspVertexList = tspVertexListparameter;

            population = new Population();
            if (newCalcStarted != null)
            {
                this.newCalcStarted(this, new TspCalcEventArgs("Starting creation of initial population of tours"));
            }
            population.CreateRandomPopulation(token, populationSize, tspVertexList, rand, chanceToUseCloseCity);
            if (newCalcStarted != null)
            {
                this.newCalcStarted(this, new TspCalcEventArgs("Initial population created"));
            }

            displayTour(population.BestTour, 0, false);

            bool foundNewBestTour = false;
            int generation;
            for (generation = 0; generation < maxGenerations; generation++)
            {
                if (newCalcStarted != null)
                {
                    this.newCalcStarted(this, new TspCalcEventArgs(generation, maxGenerations));
                }
                if (Halt)
                {
                    break;  // GUI has requested we exit.
                }
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();
                foundNewBestTour = makeChildren(groupSize, mutation);

                if (foundNewBestTour)
                {
                    displayTour(population.BestTour, generation, false);
                }
            }

            displayTour(population.BestTour, generation, true);
        }

        /// <summary>
        /// Randomly select a group of tours from the population. 
        /// The top 2 are chosen as the parent tours.
        /// Crossover is performed on these 2 tours.
        /// The childred tours from this process replace the worst 2 tours in the group.
        /// </summary>
        /// <param name="groupSize">Number of tours in this group.</param>
        /// <param name="mutation">Odds that a child will be mutated.</param>
        bool makeChildren(int groupSize, int mutation)
        {
            int[] tourGroup = new int[groupSize];
            int tourCount, i, topTour, childPosition, tempTour;
        	
            // pick random tours to be in the neighborhood city group
            // we allow for the same tour to be included twice
            for (tourCount = 0; tourCount < groupSize; tourCount++)
            {
                tourGroup[tourCount] = rand.Next(population.Count);
            }

            // bubble sort on the neighborhood city group
            for (tourCount = 0; tourCount < groupSize - 1; tourCount++)
            {
                topTour = tourCount;
                for (i = topTour + 1; i < groupSize; i++)
                {
                    if (population[tourGroup[i]].Fitness < population[tourGroup[topTour]].Fitness)
                    {
                        topTour = i;
                    }
                }

                if (topTour != tourCount)
                {
                    tempTour = tourGroup[tourCount];
                    tourGroup[tourCount] = tourGroup[topTour];
                    tourGroup[topTour] = tempTour;
                }
            }

            bool foundNewBestTour = false;

            // take the best 2 tours, do crossover, and replace the worst tour with it
            childPosition = tourGroup[groupSize - 1];
            population[childPosition] = Tour.Crossover(population[tourGroup[0]], population[tourGroup[1]], tspVertexList, rand);
            if (rand.Next(100) < mutation)
            {
                population[childPosition].Mutate(rand);
            }
            population[childPosition].DetermineFitness(tspVertexList);

            // now see if the first new tour has the best fitness
            if (population[childPosition].Fitness < population.BestTour.Fitness)
            {
                population.BestTour = population[childPosition];
                foundNewBestTour = true;
            }

            // take the best 2 tours (opposite order), do crossover, and replace the 2nd worst tour with it
            childPosition = tourGroup[groupSize - 2];
            population[childPosition] = Tour.Crossover(population[tourGroup[1]], population[tourGroup[0]], tspVertexList, rand);
            if (rand.Next(100) < mutation)
            {
                population[childPosition].Mutate(rand);
            }
            population[childPosition].DetermineFitness(tspVertexList);

            // now see if the second new tour has the best fitness
            if (population[childPosition].Fitness < population.BestTour.Fitness)
            {
                population.BestTour = population[childPosition];
                foundNewBestTour = true;
            }

            return foundNewBestTour;
        }

        /// <summary>
        /// Raise an event to the GUI listener to display a tour.
        /// </summary>
        /// <param name="bestTour">The best tour the algorithm has found so far.</param>
        /// <param name="generationNumber">How many generations have been performed.</param>
        /// <param name="complete">Is the TSP algorithm complete.</param>
        void displayTour(Tour bestTour, int generationNumber, bool complete)
        {
            if (FoundNewBestTour != null)
            {
                this.FoundNewBestTour(this, new TspEventArgs(tspVertexList, bestTour, generationNumber, complete));
            }
        }
    }
}
