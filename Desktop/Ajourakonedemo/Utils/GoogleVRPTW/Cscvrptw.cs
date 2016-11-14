// Copyright 2010-2014 Google
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ArcGISRuntime.Samples.DesktopViewer.Model;
using ArcGISRuntime.Samples.DesktopViewer.Utils;
using Catel.IoC;
using Catel.Messaging;
using Google.OrTools.ConstraintSolver;
using Microsoft.Scripting.Utils;

/// <summary>
///   Sample showing how to model and solve a capacitated vehicle routing
///   problem with time windows using the swig-wrapped version of the vehicle
///   routing library in src/constraint_solver.
/// </summary>
public class CapacitatedVehicleRoutingProblemWithTimeWindows
{

    /// <summary>
    ///   A position on the map with (x, y) coordinates.
    /// </summary>
    class Position
    {
        public Position()
        {
            this.x_ = 0;
            this.y_ = 0;
        }

        public Position(int x, int y)
        {
            this.x_ = x;
            this.y_ = y;
        }

        public int x_;
        public int y_;
    }

    public static List<List<long>> ListOfOrderLists { get; set; }

    /// <summary>
    ///    A time window with start/end data.
    /// </summary>
    class TimeWindow
    {
        public TimeWindow()
        {
            this.start_ = -1;
            this.end_ = -1;
        }

        public TimeWindow(int start, int end)
        {
            this.start_ = start;
            this.end_ = end;
        }

        public int start_;
        public int end_;
    }

    /// <summary>
    /// Manhattan distance implemented as a callback. It uses an array of
    ///   positions and computes the Manhattan distance between the two
    ///   positions of two different indices.
    /// </summary>
    class Manhattan : NodeEvaluator2
    {
        public Manhattan(Position[] locations, int coefficient, GraphVertexClass[] vertices, bool useShortestPath)
        {
            this.locations_ = locations;
            this.coefficient_ = coefficient;
            this.vertices_ = vertices;
            this.useShortestPath = useShortestPath;
        }

        public override long Run(int firstIndex, int secondIndex)
        {

            long distance = 0;
            if (firstIndex >= locations_.Length ||
                secondIndex >= locations_.Length)
            {
                return distance;
            }

            var startVertice = vertices_[firstIndex];
            var endVertice = vertices_[secondIndex];
            if (startVertice.ID == endVertice.ID)
            {
                return distance;
            }
           
            if (useShortestPath)
            {
                for(var i = 0; i < startVertice.Neighbours.Length; i++)
                {
                    if (startVertice.Neighbours[i] == endVertice.ID)
                    {
                        distance = startVertice.Distances[i];
                        return distance;
                    }
                }
               
            }
            else
            {
                distance = (Math.Abs(locations_[firstIndex].x_ - locations_[secondIndex].x_) + Math.Abs(locations_[firstIndex].y_ - locations_[secondIndex].y_)) * coefficient_;
            }
            return distance;


        }


        private Position[] locations_;
        private int coefficient_;
        private GraphVertexClass[] vertices_;
        private bool useShortestPath;
    };

    /// <summary>
    ///   A callback that computes the volume of a demand stored in an
    ///   integer array.
    /// </summary>
    class Demand : NodeEvaluator2
    {
        public Demand(int[] order_demands)
        {
            this.order_demands_ = order_demands;
        }

        public override long Run(int first_index, int second_index)
        {
            if (first_index < order_demands_.Length)
            {
                return order_demands_[first_index];
            }
            return 0;
        }

        private int[] order_demands_;
    };

    /// Locations representing either an order location or a vehicle route
    /// start/end.
    private Position[] locations_;
    /// Quantity to be picked up for each order.
    private int[] order_demands_;
    /// Time window in which each order must be performed.
    private TimeWindow[] order_time_windows_;
    /// Penalty cost "paid" for dropping an order.
    private int[] order_penalties_;
    /// Capacity of the vehicles.
    private int vehicle_capacity_ = 0;
    /// Latest time at which each vehicle must end its tour.
    private int[] vehicle_end_time_;
    /// Cost per unit of distance of each vehicle.
    private int[] vehicle_cost_coefficients_;
    /// Vehicle start and end indices. They have to be implemented as int[] due
    /// to the available SWIG-ed interface.
    private int[] vehicle_starts_;
    private int[] vehicle_ends_;

    /// Random number generator to produce data.
    private Random random_generator = new Random(0xBEEF);

    /// <summary>
    ///    Constructs a capacitated vehicle routing problem with time windows.
    /// </summary>
    internal CapacitatedVehicleRoutingProblemWithTimeWindows() { }

    /// <summary>
    ///   Creates order data. Location of the order is random, as well
    ///   as its demand (quantity), time window and penalty.  ///
    ///   </summary>
    /// <param name="number_of_orders"> number of orders to build. </param>
    /// <param name="x_max"> maximum x coordinate in which orders are located.
    /// </param>
    /// <param name="y_max"> maximum y coordinate in which orders are located.
    /// </param>
    /// <param name="demand_max"> maximum quantity of a demand. </param>
    /// <param name="time_window_max"> maximum starting time of the order time
    /// window. </param>
    /// <param name="time_window_width"> duration of the order time window.
    /// </param>
    /// <param name="penalty_min"> minimum pernalty cost if order is dropped.
    /// </param>
    /// <param name="penalty_max"> maximum pernalty cost if order is dropped.
    /// </param>
    private void BuildOrders(int number_of_orders,
                             int number_of_vehicles,
                             int x_max, int y_max,
                             int demand_max,
                             int time_window_max,
                             int time_window_width,
                             int penalty_min,
                             int penalty_max)
    {
        Console.WriteLine("Building orders.");
        locations_ = new Position[number_of_orders + 2 * number_of_vehicles];



        order_demands_ = new int[number_of_orders];
        order_time_windows_ = new TimeWindow[number_of_orders];
        order_penalties_ = new int[number_of_orders];
        for (int order = 0; order < number_of_orders; ++order)
        {
            locations_[order] =
                new Position(random_generator.Next(x_max + 1),
                             random_generator.Next(y_max + 1));
            order_demands_[order] = random_generator.Next(demand_max + 1);
            int time_window_start = random_generator.Next(time_window_max + 1);
            order_time_windows_[order] =
                new TimeWindow(time_window_start,
                               time_window_start + time_window_width);
            order_penalties_[order] =
                random_generator.Next(penalty_max - penalty_min + 1) + penalty_min;
        }
    }

    private void GetOrdersFromGraphNodes(int vehicles)
    {
        var numberOfVertices = Vertices.Length; ;
        locations_ = new Position[numberOfVertices + vehicles * 2];
        order_demands_ = new int[numberOfVertices];
        order_demands_ = new int[numberOfVertices];
        order_time_windows_ = new TimeWindow[numberOfVertices];
        order_penalties_ = new int[numberOfVertices];



        for (int index = 0; index < numberOfVertices; ++index)
        {
            var vertice = Vertices[index];
            if (vertice != null)
            {
                locations_[index] = new Position(Convert.ToInt32(vertice.X), Convert.ToInt32(vertice.Y));
                order_demands_[index] = 1;
                order_time_windows_[index] = new TimeWindow(0, 50000);
                order_penalties_[index] = 5000;
            }
            else
            {
                Console.WriteLine("Vertice not found");
            }
        }
    }

    ///  <summary>
    ///  Creates fleet data. Vehicle starting and ending locations are
    ///  random, as well as vehicle costs per distance unit.
    ///  </summary>
    /// 
    ///  <param name="number_of_orders"> number of orders</param>
    ///  <param name="number_of_vehicles"> number of vehicles</param>
    /// <param name="end_time"> latest end time of a tour of a vehicle. </param>
    ///  <param name="capacity"> capacity of a vehicle. </param>
    ///  <param name="cost_coefficient_max"> maximum cost per distance unit of a
    ///  vehicle (minimum is 1)</param>
    private void BuildFleet(int number_of_orders,
                            int number_of_vehicles,
                            int end_time,
                            int capacity,
                            int cost_coefficient_max)
    {
        Console.WriteLine("Building fleet.");
        vehicle_capacity_ = capacity;
        vehicle_starts_ = new int[number_of_vehicles];
        vehicle_ends_ = new int[number_of_vehicles];
        var staringPosition = new Position(Convert.ToInt32(StartVertice.X), Convert.ToInt32(StartVertice.Y));
        vehicle_end_time_ = new int[number_of_vehicles];
        vehicle_cost_coefficients_ = new int[number_of_vehicles];
        for (int vehicle = 0; vehicle < number_of_vehicles; ++vehicle)
        {
            int index = 2 * vehicle + number_of_orders;
            vehicle_starts_[vehicle] = index;
            locations_[index] = staringPosition;

            vehicle_ends_[vehicle] = index + 1;
            locations_[index + 1] = staringPosition;
            vehicle_end_time_[vehicle] = end_time;
            vehicle_cost_coefficients_[vehicle] =
                random_generator.Next(cost_coefficient_max) + 1;
        }
    }


  
    /// <summary>
    ///   Solves the current routing problem.
    /// </summary>
    private void Solve(int number_of_orders, int number_of_vehicles, GraphVertexClass[] verticeList, bool useShortestPath)
    {
        Console.WriteLine("Creating model with " + number_of_orders +
                          " orders and " + number_of_vehicles + " vehicles.");
        // Finalizing model
        int number_of_locations = locations_.Length;

        RoutingModel model =
            new RoutingModel(number_of_locations, number_of_vehicles,
                             vehicle_starts_, vehicle_ends_);

        // Setting up dimensions
        const int big_number = 100000;
        var manhattan_callback = new Manhattan(locations_, 1, verticeList, useShortestPath);
        model.AddDimension(
            manhattan_callback, big_number, big_number, false, "time");
        NodeEvaluator2 demand_callback = new Demand(order_demands_);
        model.AddDimension(demand_callback, 0, vehicle_capacity_, true, "capacity");

        // Setting up vehicles
        for (int vehicle = 0; vehicle < number_of_vehicles; ++vehicle)
        {
            int cost_coefficient = vehicle_cost_coefficients_[vehicle];
            var manhattan_cost_callback = new Manhattan(locations_, cost_coefficient, verticeList, useShortestPath);
            model.SetVehicleCost(vehicle, manhattan_cost_callback);
            model.CumulVar(model.End(vehicle), "time").SetMax(
                vehicle_end_time_[vehicle]);
        }

        // Setting up orders
        for (int order = 0; order < number_of_orders; ++order)
        {
            model.CumulVar(order, "time").SetRange(order_time_windows_[order].start_,
                                                   order_time_windows_[order].end_);
            int[] orders = { order };
            model.AddDisjunction(orders, order_penalties_[order]);
        }

        // Solving
        RoutingSearchParameters search_parameters = RoutingModel.DefaultSearchParameters();
        search_parameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.AllUnperformed;

        Console.WriteLine("Search");
        Assignment solution = model.SolveWithParameters(search_parameters);

        if (solution != null)
        {
            String output = "Total cost: " + solution.ObjectiveValue() + "\n";
            // Dropped orders
            String dropped = "";
            for (int order = 0; order < number_of_orders; ++order)
            {
                if (solution.Value(model.NextVar(order)) == order)
                {
                    dropped += " " + order;
                }
            }
            if (dropped.Length > 0)
            {
                output += "Dropped orders:" + dropped + "\n";
            }
            // Routes
            ListOfOrderLists = new List<List<long>>();
            for (int vehicle = 0; vehicle < number_of_vehicles; ++vehicle)
            {
                String route = "Vehicle " + vehicle + ": ";
                var orderlist = new List<long>();
                long order = model.Start(vehicle);

                if (model.IsEnd(solution.Value(model.NextVar(order))))
                {
                    route += "Empty";
                }
                else
                {
                    for (;
                         !model.IsEnd(order);
                         order = solution.Value(model.NextVar(order)))
                    {
                        orderlist.Add(order);
                        IntVar local_load = model.CumulVar(order, "capacity");
                        IntVar local_time = model.CumulVar(order, "time");
                        route += order + " Load(" + solution.Value(local_load) + ") " +
                            "Time(" + solution.Min(local_time) + ", " +
                            solution.Max(local_time) + ") -> ";
                    }
                    IntVar load = model.CumulVar(order, "capacity");
                    IntVar time = model.CumulVar(order, "time");
                    orderlist.Add(order);
                    route += order + " Load(" + solution.Value(load) + ") " +
                        "Time(" + solution.Min(time) + ", " + solution.Max(time) + ")";
                }


                output += route + "\n";
                ListOfOrderLists.Add(orderlist);
            }
            Console.WriteLine(output);
        }
    }



    public static bool Start(List<GraphVertexClass> vertices, int vertexGroupSize, bool useShortestPath)
    {
        if (vertices == null || vertices.Count < 4)
        {

            return false;
        }
        StartVertice = vertices.Last();
        vertices.RemoveAt(vertices.Count -1);
        Vertices = vertices.ToArray();
     
        CapacitatedVehicleRoutingProblemWithTimeWindows problem =
            new CapacitatedVehicleRoutingProblemWithTimeWindows();
        int x_max = 1000000;
        int y_max = 1000000;
        int demand_max = 3;
        int time_window_max = 24 * 1000;
        int time_window_width = 4 * 2000;
        int penalty_min = 50000;
        int penalty_max = 50000;
        int end_time = 24 * 600000;
        int cost_coefficient_max = 3;



        int orders = Vertices.Length;

        int capacity = vertexGroupSize;
        int vehicles = (orders - 1) / capacity + 1;

        var verticeList = new List<GraphVertexClass>();
        verticeList.AddRange(Vertices);
        var i = 0;
        while (i < vehicles)
        {
            verticeList.Add(StartVertice);
            verticeList.Add(StartVertice);
            i++;
        }


        problem.GetOrdersFromGraphNodes(vehicles);
        //problem.SetVehicle();
        //problem.BuildOrders(orders,
        //                    vehicles,
        //                    x_max,
        //                    y_max,
        //                    demand_max,
        //                    time_window_max,
        //                    time_window_width,
        //                    penalty_min,
        //                    penalty_max);
        problem.BuildFleet(orders,
                           vehicles,
                           end_time,
                           capacity,
                           cost_coefficient_max);
        problem.Solve(orders, vehicles, verticeList.ToArray(), useShortestPath);

      


        foreach (var orderlist in ListOfOrderLists)
        {
            MapUtils.Instance.GraphicsLayer.ClearSelection();
            MapUtils.Instance.DrawRouteFromOrderList(orderlist.ToArray(), verticeList.ToArray(), useShortestPath);
            MapUtils.Instance.ShowGeneralizedRoutes(MapUtils.Instance.GraphicsLayer.SelectedGraphics, false);
        }


        return true;
    }

    public static GraphVertexClass StartVertice
    { get; set; }

    public static GraphVertexClass[] Vertices { get; set; }

    private void SetVehicle()
    {
        var numberOfVertices = GraphUtils.Instance.Graph.Vertices.Count();
        var startVertice = GraphUtils.Instance.Graph.Vertices.FirstOrDefault();
        var staringPosition = new Position(Convert.ToInt32(startVertice.X), Convert.ToInt32(startVertice.Y));
        vehicle_capacity_ = 1000;
        vehicle_starts_ = new int[1];
        vehicle_ends_ = new int[1];
        vehicle_end_time_ = new int[1];
        vehicle_cost_coefficients_ = new int[1];
        for (int vehicle = 0; vehicle < 1; ++vehicle)
        {
            int index = 2 * vehicle + numberOfVertices;
            vehicle_starts_[vehicle] = index;
            locations_[index] = staringPosition;
            vehicle_ends_[vehicle] = index + 1;

            vehicle_cost_coefficients_[vehicle] = 1;
        }
    }
}