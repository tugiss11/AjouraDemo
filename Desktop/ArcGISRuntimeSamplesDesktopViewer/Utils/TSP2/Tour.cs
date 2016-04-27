//////////////////////////////////////////////////////////////////////////////////////////////////
// File Name: Tour.cs
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
using System.Text;
using ArcGISRuntime.Samples.DesktopViewer.Utils;
using ArcGISRuntime.Samples.DesktopViewer.Utils.TSP2;

namespace Tsp
{
    /// <summary>
    /// This class represents one instance of a tour through all the TSPVertices.
    /// </summary>
    public class Tour : List<TSPEdge>
    {
        /// <summary>
        /// Constructor that takes a default capacity.
        /// </summary>
        /// <param name="capacity">Initial size of the tour. Should be the number of TSPVertices in the tour.</param>
        public Tour(int capacity)
            : base(capacity)
        {
            resetTour(capacity);
        }

        /// <summary>
        /// Private copy of this fitness of this tour.
        /// </summary>
        private double fitness;
        /// <summary>
        /// The fitness (total tour length) of this tour.
        /// </summary>
        public double Fitness
        {
            set
            {
                fitness = value;
            }
            get
            {
                return fitness;
            }
        }

        /// <summary>
        /// Creates the tour with the correct number of TSPVertices and creates initial connections of all -1.
        /// </summary>
        /// <param name="numberOfCities"></param>
        private void resetTour(int numberOfCities)
        {
            this.Clear();

            TSPEdge tspEdge;
            for (int i = 0; i < numberOfCities; i++)
            {
                tspEdge = new TSPEdge();
                tspEdge.Connection1 = -1;
                tspEdge.Connection2 = -1;
                this.Add(tspEdge);
            }
        }

        /// <summary>
        /// Determine the fitness (total length) of an individual tour.
        /// </summary>
        /// <param name="tspVertices">The TSPVertices in this tour. Used to get the distance between each city.</param>
        public void DetermineFitness(TSPVertices tspVertices)
        {
            Fitness = 0;
            GraphUtils.Instance.ResetVisited();
            int lastCity = 0;
            int nextCity = this[0].Connection1;

            foreach (TSPEdge link in this)
            {
                double edgeFitness;
                if (GraphUtils.Instance.UseVisitedEdges)
                {
                    edgeFitness = GraphUtils.Instance.GetDistance(tspVertices[lastCity], tspVertices[nextCity]);
                }
                else
                {
                    edgeFitness = tspVertices[lastCity].Distances[nextCity];
                }
                Fitness += edgeFitness;
              

                

                // figure out if the next city in the list is [0] or [1]
                if (lastCity != this[nextCity].Connection1)
                {
                    lastCity = nextCity;
                    nextCity = this[nextCity].Connection1;
                }
                else
                {
                    lastCity = nextCity;
                    nextCity = this[nextCity].Connection2;
                }
            }
        }

        /// <summary>
        /// Creates a link between 2 TSPVertices in a tour, and then updates the city usage.
        /// </summary>
        /// <param name="tour">The incomplete child tour.</param>
        /// <param name="cityUsage">Number of times each city has been used in this tour. Is updated when TSPVertices are joined.</param>
        /// <param name="city1">The first city in the link.</param>
        /// <param name="city2">The second city in the link.</param>
        private static void joinCities(Tour tour, int[] cityUsage, int city1, int city2)
        {
            // Determine if the [0] or [1] link is available in the tour to make this link.
            if (tour[city1].Connection1 == -1)
            {
                tour[city1].Connection1 = city2;
            }
            else
            {
                tour[city1].Connection2 = city2;
            }

            if (tour[city2].Connection1 == -1)
            {
                tour[city2].Connection1 = city1;
            }
            else
            {
                tour[city2].Connection2 = city1;
            }

            cityUsage[city1]++;
            cityUsage[city2]++;
        }


        /// <summary>
        /// Find a link from a given city in the parent tour that can be placed in the child tour.
        /// If both links in the parent aren't valid links for the child tour, return -1.
        /// </summary>
        /// <param name="parent">The parent tour to get the link from.</param>
        /// <param name="child">The child tour that the link will be placed in.</param>
        /// <param name="tspVertexList">The list of TSPVertices in this tour.</param>
        /// <param name="cityUsage">Number of times each city has been used in the child.</param>
        /// <param name="city">TSPVertice that we want to link from.</param>
        /// <returns>The city to link to in the child tour, or -1 if none are valid.</returns>
        private static int findNextCity(Tour parent, Tour child, TSPVertices tspVertexList, int[] cityUsage, int city)
        {
            if (testConnectionValid(child, tspVertexList, cityUsage, city, parent[city].Connection1))
            {
                return parent[city].Connection1;
            }
            else if (testConnectionValid(child, tspVertexList, cityUsage, city, parent[city].Connection2))
            {
                return parent[city].Connection2;
            }

            return -1;
        }

        /// <summary>
        /// Determine if it is OK to connect 2 TSPVertices given the existing connections in a child tour.
        /// If the two TSPVertices can be connected already (witout doing a full tour) then it is an invalid link.
        /// </summary>
        /// <param name="tour">The incomplete child tour.</param>
        /// <param name="tspVertexList">The list of TSPVertices in this tour.</param>
        /// <param name="cityUsage">Array that contains the number of times each city has been linked.</param>
        /// <param name="city1">The first city in the link.</param>
        /// <param name="city2">The second city in the link.</param>
        /// <returns>True if the connection can be made.</returns>
        private static bool testConnectionValid(Tour tour, TSPVertices tspVertexList, int[] cityUsage, int city1, int city2)
        {
            // Quick check to see if TSPVertices already connected or if either already has 2 links
            if ((city1 == city2) || (cityUsage[city1] == 2) || (cityUsage[city2] == 2))
            {
                return false;
            }

            // A quick check to save CPU. If haven't been to either city, connection must be valid.
            if ((cityUsage[city1] == 0) || (cityUsage[city2] == 0))
            {
                return true;
            }

            // Have to see if the TSPVertices are connected by going in each direction.
            for (int direction = 0; direction < 2; direction++)
            {
                int lastCity = city1;
                int currentCity;
                if (direction == 0)
                {
                    currentCity = tour[city1].Connection1;  // on first pass, use the first connection
                }
                else
                {
                    currentCity = tour[city1].Connection2;  // on second pass, use the other connection
                }
                int tourLength = 0;
                while ((currentCity != -1) && (currentCity != city2) && (tourLength < tspVertexList.Count - 2))
                {
                    tourLength++;
                    // figure out if the next city in the list is [0] or [1]
                    if (lastCity != tour[currentCity].Connection1)
                    {
                        lastCity = currentCity;
                        currentCity = tour[currentCity].Connection1;
                    }
                    else
                    {
                        lastCity = currentCity;
                        currentCity = tour[currentCity].Connection2;
                    }
                }

                // if TSPVertices are connected, but it goes through every city in the list, then OK to join.
                if (tourLength >= tspVertexList.Count - 2)
                {
                    return true;
                }

                // if the TSPVertices are connected without going through all the TSPVertices, it is NOT OK to join.
                if (currentCity == city2)
                {
                    return false;
                }
            }

            // if TSPVertices weren't connected going in either direction, we are OK to join them
            return true;
        }

        /// <summary>
        /// Perform the crossover operation on 2 parent tours to create a new child tour.
        /// This function should be called twice to make the 2 children.
        /// In the second call, the parent parameters should be swapped.
        /// </summary>
        /// <param name="parent1">The first parent tour.</param>
        /// <param name="parent2">The second parent tour.</param>
        /// <param name="tspVertexList">The list of TSPVertices in this tour.</param>
        /// <param name="rand">Random number generator. We pass around the same random number generator, so that results between runs are consistent.</param>
        /// <returns>The child tour.</returns>
        public static Tour Crossover(Tour parent1, Tour parent2, TSPVertices tspVertexList, Random rand)
        {
            Tour child = new Tour(tspVertexList.Count);      // the new tour we are making
            int[] cityUsage = new int[tspVertexList.Count];  // how many links 0-2 that connect to this city
            int city;                                   // for loop variable
            int nextCity;                               // the other city in this link

            for (city = 0; city < tspVertexList.Count; city++)
            {
                cityUsage[city] = 0;
            }

            // Take all links that both parents agree on and put them in the child
            for (city = 0; city < tspVertexList.Count; city++)
            {
                if (cityUsage[city] < 2)
                {
                    if (parent1[city].Connection1 == parent2[city].Connection1)
                    {
                        nextCity = parent1[city].Connection1;
                        if (testConnectionValid(child, tspVertexList, cityUsage, city, nextCity))
                        {
                            joinCities(child, cityUsage, city, nextCity);
                        }
                    }
                    if (parent1[city].Connection2 == parent2[city].Connection2)
                    {
                        nextCity = parent1[city].Connection2;
                        if (testConnectionValid(child, tspVertexList, cityUsage, city, nextCity))
                        {
                            joinCities(child, cityUsage, city, nextCity);

                        }
                    }
                    if (parent1[city].Connection1 == parent2[city].Connection2)
                    {
                        nextCity = parent1[city].Connection1;
                        if (testConnectionValid(child, tspVertexList, cityUsage, city, nextCity))
                        {
                            joinCities(child, cityUsage, city, nextCity);
                        }
                    }
                    if (parent1[city].Connection2 == parent2[city].Connection1)
                    {
                        nextCity = parent1[city].Connection2;
                        if (testConnectionValid(child, tspVertexList, cityUsage, city, nextCity))
                        {
                            joinCities(child, cityUsage, city, nextCity);
                        }
                    }
                }
            }

            // The parents don't agree on whats left, so we will alternate between using
            // links from parent 1 and then parent 2.

            for (city = 0; city < tspVertexList.Count; city++)
            {
                if (cityUsage[city] < 2)
                {
                    if (city % 2 == 1)  // we prefer to use parent 1 on odd TSPVertices
                    {
                        nextCity = findNextCity(parent1, child, tspVertexList, cityUsage, city);
                        if (nextCity == -1) // but if thats not possible we still go with parent 2
                        {
                            nextCity = findNextCity(parent2, child, tspVertexList, cityUsage, city); ;
                        }
                    }
                    else // use parent 2 instead
                    {
                        nextCity = findNextCity(parent2, child, tspVertexList, cityUsage, city);
                        if (nextCity == -1)
                        {
                            nextCity = findNextCity(parent1, child, tspVertexList, cityUsage, city);
                        }
                    }

                    if (nextCity != -1)
                    {
                        joinCities(child, cityUsage, city, nextCity);

                        // not done yet. must have been 0 in above case.
                        if (cityUsage[city] == 1)
                        {
                            if (city % 2 != 1)  // use parent 1 on even TSPVertices
                            {
                                nextCity = findNextCity(parent1, child, tspVertexList, cityUsage, city);
                                if (nextCity == -1) // use parent 2 instead
                                {
                                    nextCity = findNextCity(parent2, child, tspVertexList, cityUsage, city);
                                }
                            }
                            else // use parent 2
                            {
                                nextCity = findNextCity(parent2, child, tspVertexList, cityUsage, city);
                                if (nextCity == -1)
                                {
                                    nextCity = findNextCity(parent1, child, tspVertexList, cityUsage, city);
                                }
                            }

                            if (nextCity != -1)
                            {
                                joinCities(child, cityUsage, city, nextCity);
                            }
                        }
                    }
                }
            }

            // Remaining links must be completely random.
            // Parent's links would cause multiple disconnected loops.
            for (city = 0; city < tspVertexList.Count; city++)
            {
                while (cityUsage[city] < 2)
                {
                    do
                    {
                        nextCity = rand.Next(tspVertexList.Count);  // pick a random city, until we find one we can link to
                    } while (!testConnectionValid(child, tspVertexList, cityUsage, city, nextCity));

                    joinCities(child, cityUsage, city, nextCity);
                }
            }

            return child;
        }

        /// <summary>
        /// Randomly change one of the links in this tour.
        /// </summary>
        /// <param name="rand">Random number generator. We pass around the same random number generator, so that results between runs are consistent.</param>
        public void Mutate(Random rand)
        {
            int cityNumber = rand.Next(this.Count);
            TSPEdge tspEdge = this[cityNumber];
            int tmpCityNumber;

            // Find which 2 TSPVertices connect to cityNumber, and then connect them directly
            if (this[tspEdge.Connection1].Connection1 == cityNumber)   // Conn 1 on Conn 1 TSPEdge points back to us.
            {
                if (this[tspEdge.Connection2].Connection1 == cityNumber)// Conn 1 on Conn 2 TSPEdge points back to us.
                {
                    tmpCityNumber = tspEdge.Connection2;
                    this[tspEdge.Connection2].Connection1 =tspEdge.Connection1;
                    this[tspEdge.Connection1].Connection1 = tmpCityNumber;
                }
                else                                                // Conn 2 on Conn 2 TSPEdge points back to us.
                {
                    tmpCityNumber = tspEdge.Connection2;
                    this[tspEdge.Connection2].Connection2 = tspEdge.Connection1;
                    this[tspEdge.Connection1].Connection1 = tmpCityNumber;
                }
            }
            else                                                    // Conn 2 on Conn 1 TSPEdge points back to us.
            {
                if (this[tspEdge.Connection2].Connection1 == cityNumber)// Conn 1 on Conn 2 TSPEdge points back to us.
                {
                    tmpCityNumber = tspEdge.Connection2;
                    this[tspEdge.Connection2].Connection1 = tspEdge.Connection1;
                    this[tspEdge.Connection1].Connection2 = tmpCityNumber;
                }
                else                                                // Conn 2 on Conn 2 TSPEdge points back to us.
                {
                    tmpCityNumber = tspEdge.Connection2;
                    this[tspEdge.Connection2].Connection2 = tspEdge.Connection1;
                    this[tspEdge.Connection1].Connection2 = tmpCityNumber;
                }

            }

            int replaceCityNumber = -1;
            do
            {
                replaceCityNumber = rand.Next(this.Count);
            }
            while (replaceCityNumber == cityNumber);
            TSPEdge replaceTSPEdge = this[replaceCityNumber];

            // Now we have to reinsert that city back into the tour at a random location
            tmpCityNumber = replaceTSPEdge.Connection2;
            tspEdge.Connection2 = replaceTSPEdge.Connection2;
            tspEdge.Connection1 = replaceCityNumber;
            replaceTSPEdge.Connection2 = cityNumber;

            if (this[tmpCityNumber].Connection1 == replaceCityNumber)
            {
                this[tmpCityNumber].Connection1 = cityNumber;
            }
            else
            {
                this[tmpCityNumber].Connection2 = cityNumber;
            }
        }
    }
}
