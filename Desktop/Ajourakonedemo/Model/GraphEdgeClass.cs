using System.ComponentModel;
using QuickGraph;

namespace ArcGISRuntime.Samples.DesktopViewer.Model
{
    public class GraphEdgeClass : Edge<GraphVertexClass>, INotifyPropertyChanged
    {
        private int _id;

        public int Id
        {
            get { return _id; }
            set
            {
                _id = value;
                NotifyPropertyChanged("ID");
            }
        }

        public double Sivukaltevuus { get; set;}
        public int Kosteus { get; set; }
        public double Weight { get; set; }
        public bool IsVisited { get; set;} 

        public GraphEdgeClass(int id, double sivukaltevuus, int kosteus, GraphVertexClass source, GraphVertexClass target)
            : base(source, target)
        {
            Id = id;
            Kosteus = kosteus;
            Sivukaltevuus = sivukaltevuus;
        }

    

        public GraphEdgeClass(int id, GraphVertexClass source, GraphVertexClass target)
          : base(source, target)
        {
            Id = id;
        }

        public override string ToString()
        {
            return Id.ToString();
        }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion
    }


}
