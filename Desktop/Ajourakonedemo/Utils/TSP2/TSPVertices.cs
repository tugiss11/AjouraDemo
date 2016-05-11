//////////////////////////////////////////////////////////////////////////////////////////////////
// File Name: TSPVertices.cs
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
using ArcGISRuntime.Samples.DesktopViewer.Model;
using Catel.IoC;
using Catel.Logging;
using Catel.Messaging;

namespace ArcGISRuntime.Samples.DesktopViewer.Utils.TSP2
{
    /// <summary>
    /// This class contains the list of cities for this test.
    /// Each city has a location and the distance information to every other city.
    /// </summary>
    public class TSPVertices : List<TSPVertice>
    {

        protected readonly ILog Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Determine the distances between each city.
        /// </summary>
        /// <param name="ofCloseCities"></param>
        /// <param name="token"></param>
        /// <param name="numberOfCloseCities">When creating the initial population of tours, this is a greater chance
        ///     that a nearby city will be chosen for a link. This is the number of nearby cities that will be considered close.</param>
        public void CalculateVertexDistances(CancellationToken token, int numberOfCloseCities)
        {
            var capacity = this.Count*this.Count;
            var shortestPathList = new List<ShortestPath>(capacity);
            var j = 0;
            this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage(string.Format("Calculating shortest paths for {0} vertices (takes about 30 seconds for 271 points)", this.Count), "UpdateStatusBar");
            var sw2 = new Stopwatch();
            sw2.Start();
            //270 pisteelle 60s vs 30s
            Parallel.ForEach(this, (vertexToVisit) =>
            {
                j++;
                var tempList = GetDistances(vertexToVisit, GraphUtils.Instance.Graph.Vertices.ToList());
                shortestPathList.AddRange(tempList);
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();
            });
            sw2.Stop();
            //var sw1 = new Stopwatch();
            //shortestPathList.Clear();
            //sw1.Start();
            //foreach (var tempList in this.Select(vertex => GetDistances(vertex, GraphUtils.Instance.Graph.Vertices.ToList())))
            //{
            //    shortestPathList.AddRange(tempList);
            //    if (token.IsCancellationRequested)
            //        token.ThrowIfCancellationRequested();
            //}
            //sw1.Stop();
          
            GraphUtils.Instance.ShortestPathList = shortestPathList;
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, 
                new Action(() => 
                this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage(string.Format("Total paths: {0}", GraphUtils.Instance.ShortestPathList.Count), "NaytaInfoboksiKayttajalle")));
           
            foreach (TSPVertice vertice in this)
            {
                vertice.FindClosestVertices(numberOfCloseCities);
            }
        }

        public List<ShortestPath> GetDistances(TSPVertice vertexToVisit, List<GraphVertexClass> allVertices)
        {
            vertexToVisit.Distances.Clear();
            var shortestPathList = new List<ShortestPath>();
            for (int i = 0; i < this.Count; i++)
            {
                var startVertice = allVertices.FirstOrDefault(o => o.ID == vertexToVisit.Id);
                var endVertice = allVertices.FirstOrDefault(o => o.ID == this[i].Id);

                try
                {
                    //this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage(string.Format("Calculating shortest paths for vertex {0}/{1}, total paths calculated: {2}", j, this.Count, shortestPathList.Count), "UpdateStatusBar");
                    //ShortestPath path = shortestPathList.FirstOrDefault(o => (o.VertexId1 == startVertice.ID && o.VertexId2 == endVertice.ID) || (o.VertexId1 == endVertice.ID && o.VertexId2 == startVertice.ID));
                    //if (path != null)
                    //{
                    //    vertexToVisit.Distances.Add(path.Distance);
                    //}
                    //else
                    //{
                    var edges = GraphUtils.Instance.ShortestPathAlgorithm(startVertice, endVertice);

                    if (edges.Any())
                    {
                        var distance = edges.Aggregate(0, (current, edge) => current + Convert.ToInt32(edge.Weight));
                        //var distance = edges.Count*17;
                        vertexToVisit.Distances.Add(distance);
                        shortestPathList.Add(new ShortestPath { ShortestPathEdges = edges, VertexId1 = startVertice.ID, VertexId2 = endVertice.ID, Distance = distance });
                   } 
                    else
                    {
                        vertexToVisit.Distances.Add(0.0);
                    }
                    //}
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }
            return shortestPathList;
        }
    }
}
