using System.Globalization;

namespace AfvalOphaler
{
    public class Order
    {
        public int OrderId;
        public string Name;
        public int Frequency;
        public int NumContainers;
        public int VolPerContainer;
        public double TimeToEmpty;
        public int MatrixId;
        public int XCoord;
        public int YCoord;
        public double JourneyTimeFromDump;
        public double JourneyTimeToDump;
        public double Score;
        public int Cluster;

        public Order (string[] row)
        {
            OrderId = int.Parse(row[0]);
            Name = row[1].Trim();
            Frequency = row[2][0] - '0';
            NumContainers = int.Parse(row[3]);
            VolPerContainer = int.Parse(row[4]);
            TimeToEmpty = double.Parse(row[5], CultureInfo.InvariantCulture);
            MatrixId = int.Parse(row[6]);
            XCoord = int.Parse(row[7]);
            YCoord = int.Parse(row[8]);
            JourneyTimeFromDump = GD.JourneyTime[GD.Dump.MatrixId, MatrixId];
            JourneyTimeToDump = GD.JourneyTime[MatrixId, GD.Dump.MatrixId];
            Score = JourneyTimeToDump;
            //Score = (JourneyTimeToDump + 1) / TimeToEmpty;
        }

        public Order() { }

        public override string ToString()
        {
            return $"oid{OrderId}; mid{MatrixId}; s{Score}; jttd{JourneyTimeToDump}; jtfd{JourneyTimeFromDump}; f{Frequency}PWK; nc{NumContainers}; vpc{VolPerContainer}; tte{TimeToEmpty}; x{XCoord}; y{YCoord}; {Name}";
        }
    }
}
