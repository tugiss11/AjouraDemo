//////////////////////////////////////////////////////////////////////////////////////////////////
// File Name: TspEventArgs.cs
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
using Tsp;

namespace ArcGISRuntime.Samples.DesktopViewer.Utils.TSP2
{
    /// <summary>
    /// Event arguments when the TSP class wants the GUI to draw a tour.
    /// </summary>
    public class TspCalcEventArgs : EventArgs
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public TspCalcEventArgs()
        {
        }


        public TspCalcEventArgs(int onGoingNumber, int maxCount)
        {
            max = maxCount;
            onGoing = onGoingNumber;
        }

        public TspCalcEventArgs(string message)
        {
            _message = message;
        }

        private string _message;

        public string Message
        {
            get
            {
                return _message;
            }
        }

        private int onGoing;
       
        public int OnGoing
        {
            get
            {
                return onGoing;
            }
        }

        private int max;

        public int Max
        {
            get
            {
                return max;
            }
        }

    }
}