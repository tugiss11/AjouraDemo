
namespace ArcGISRuntime.Samples.DesktopViewer.Utils.TSP
{
	public class TSPVertex
	{
		protected bool Equals(TSPVertex other)
		{
			return Y.Equals(other.Y) && X.Equals(other.X);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (Y.GetHashCode()*397) ^ X.GetHashCode();
			}
		}

		public TSPVertex()
		{}

		public TSPVertex(string name, double y, double x)
		{
			Name = name;
			Y = y;
			X = x;
		}

		public string Name;

		public double Y { get; set; }
		public double X { get; set; }

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((TSPVertex) obj);
		}
	}
}