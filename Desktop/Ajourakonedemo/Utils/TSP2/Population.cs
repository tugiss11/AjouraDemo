//////////////////////////////////////////////////////////////////////////////////////////////////
// File Name: Population.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Catel.IoC;
using Catel.Logging;
using Catel.Messaging;
using Tsp;

namespace ArcGISRuntime.Samples.DesktopViewer.Utils.TSP2
{
    class Population : List<Tour>
    {
        protected readonly ILog log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Private copy of the best tour found so far by the Genetic Algorithm.
        /// </summary>
        private Tour bestTour = null;

        /// <summary>
        /// The best tour found so far by the Genetic Algorithm.
        /// </summary>
        public Tour BestTour
        {
            set { bestTour = value; }
            get { return bestTour; }
        }

        /// <summary>
        /// Create the initial set of random tours.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="populationSize">Number of tours to create.</param>
        /// <param name="tspVertexList">The list of TSPVertices in this tour.</param>
        /// <param name="rand">Random number generator. We pass around the same random number generator, so that results between runs are consistent.</param>
        /// <param name="chanceToUseCloseCity">The odds (out of 100) that a city that is known to be close will be used in any given link.</param>
        public void CreateRandomPopulation(CancellationToken token, int populationSize, TSPVertices tspVertexList, Random rand, int chanceToUseCloseCity)
        {
            var sw = new Stopwatch();
            var sw2 = new Stopwatch();
           

            Clear();
            sw.Start();
            for (int tourCount = 0; tourCount < populationSize; tourCount++)
            {
                Add(CreateRandomTour(token, tspVertexList, rand, chanceToUseCloseCity));
            }
            sw.Stop();
            sw2.Start();
            var bestFitness = this.Min(o => o.Fitness);
            BestTour = this.FirstOrDefault(o => Math.Abs(o.Fitness - bestFitness) < 0.01);
            sw2.Stop();
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() =>
                this.GetDependencyResolver().Resolve<IMessageMediator>()
                .SendMessage(string.Format("Created population in time {0} + {1} ms", sw.ElapsedMilliseconds, sw2.ElapsedMilliseconds), "NaytaInfoboksiKayttajalle")));


            //for (int tourCount = 0; tourCount < populationSize; tourCount++)
            //{
            //    var count = tourCount;
            //    this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage(string.Format("Creating initial random population {0}/{1}", count, populationSize), "UpdateStatusBar");
            //    Tour tour = new Tour(tspVertexList.Count);
            //    if (token.IsCancellationRequested)
            //        token.ThrowIfCancellationRequested();

            //    // Create a starting point for this tor
            //    firstCity = rand.Next(tspVertexList.Count);
            //    lastCity = firstCity;

            //    for (int city = 0; city < tspVertexList.Count - 1; city++)
            //    {
            //        do
            //        {
            //            // Keep picking random TSPVertices for the next city, until we find one we haven't been to.
            //            if ((rand.Next(100) < chanceToUseCloseCity) && (tspVertexList[city].CloseCities.Count > 0))
            //            {
            //                // 75% chance will will pick a city that is close to this one
            //                nextCity = tspVertexList[city].CloseCities[rand.Next(tspVertexList[city].CloseCities.Count)];
            //            }
            //            else
            //            {
            //                // Otherwise, pick a completely random city.
            //                nextCity = rand.Next(tspVertexList.Count);
            //            }
            //            // Make sure we haven't been here, and make sure it isn't where we are at now.
            //        } while ((tour[nextCity].Connection2 != -1) || (nextCity == lastCity));

            //        // When going from city A to B, [1] on A = B and [1] on city B = A
            //        tour[lastCity].Connection2 = nextCity;
            //        tour[nextCity].Connection1 = lastCity;
            //        lastCity = nextCity;
            //    }

            //    // Connect the last 2 TSPVertices.
            //    tour[lastCity].Connection2 = firstCity;
            //    tour[firstCity].Connection1 = lastCity;

            //    tour.DetermineFitness(tspVertexList);

            //    Add(tour);

            //    if ((bestTour == null) || (tour.Fitness < bestTour.Fitness))
            //    {
            //        BestTour = tour;
            //    }
            //}

        }

        private Tour CreateRandomTour(CancellationToken token, List<TSPVertice> tspVertexList, Random rand, int chanceToUseCloseCity)
        {
            try
            {

                int nextCity;
                Tour tour = new Tour(tspVertexList.Count);
                var firstCity = rand.Next(tspVertexList.Count);
                var lastCity = firstCity;
                for (int city = 0; city < tspVertexList.Count - 1; city++)
                {
                   
                    do
                    {
                        if (token.IsCancellationRequested)
                            token.ThrowIfCancellationRequested();
                        // Keep picking random TSPVertices for the next city, until we find one we haven't been to.
                        if ((rand.Next(100) < chanceToUseCloseCity) && (tspVertexList[city].CloseCities.Count > 0))
                        {
                            // 75% chance will will pick a city that is close to this one
                            nextCity = tspVertexList[city].CloseCities[rand.Next(tspVertexList[city].CloseCities.Count)];
                        }
                        else
                        {
                            // Otherwise, pick a completely random city.
                            nextCity = rand.Next(tspVertexList.Count);
                        }
                        // Make sure we haven't been here, and make sure it isn't where we are at now.
                    } while ((tour[nextCity].Connection2 != -1) || (nextCity == lastCity));

                    // When going from city A to B, [1] on A = B and [1] on city B = A
                    tour[lastCity].Connection2 = nextCity;
                    tour[nextCity].Connection1 = lastCity;
                    lastCity = nextCity;
                }

                // Connect the last 2 TSPVertices.
                tour[lastCity].Connection2 = firstCity;
                tour[firstCity].Connection1 = lastCity;

                tour.DetermineFitness(tspVertexList);
                return tour;
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw;
            }
        }
    }
}
