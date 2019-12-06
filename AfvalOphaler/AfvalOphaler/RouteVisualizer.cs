using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace AfvalOphaler
{
    public partial class RouteVisualizer : Form
    {
        /*
         * Input Range:
         *  min_x: 55297404, max_x: 59217110, delta: 3919706
         *  min_y: 512345518, max_y: 515539852, delta: 3194334
         *  
         * Stort:
         *  (56343016,513026712)
         */

        List<Point> orderPoints;
        List<Order> orders;
        Point dump = new Point(56343016, 513026712);
        Bitmap map;

        public RouteVisualizer(List<Point> _orders)
        {
            orderPoints = _orders;
            InitializeComponent();
            Show();
            //RouteVisualizer_SizeChanged(null, null);
        }
        public RouteVisualizer(List<Order> _orders, int _clusterCount)
        {
            orders = _orders;
            clusterCount = _clusterCount;
            InitializeComponent();
            Show();
        }

        int clusterCount;
        void InitializeClusterMap()
        {
            //Color[] clusterColors = CreateClusterColors();
            Color[] clusterColors = new Color[]
            {
                Color.Red,
                Color.Blue,
                Color.Green,
                Color.Yellow,
                Color.Purple,
                Color.Cyan,
                Color.Orange,
                Color.White,
                Color.Pink,
                Color.Brown
            };
            xmap = ClientSize.Width / 3919706.0;
            ymap = ClientSize.Height / 3194334.0;
            map = new Bitmap(ClientSize.Width, ClientSize.Height);
            mapbox.Image = map;
            foreach (Order o in orders)
            {
                Point mapped_o = Map2Map(o.XCoord, o.YCoord);
                //Console.WriteLine($"Writing pixel: {mapped_o.X},{mapped_o.Y}");
                map.SetPixel(mapped_o.X, mapped_o.Y, clusterColors[o.Cluster]);
            }
            PaintDump();
        }
        Color[] CreateClusterColors()
        {
            Random rnd = new Random();
            Color[] colors = new Color[clusterCount];
            for (int i = 0; i < clusterCount; i++)
            {
                colors[i] = Color.FromArgb(rnd.Next(127, 256), rnd.Next(127, 256), rnd.Next(127, 256));
            }
            return colors;
        }

        void InitializeMap()
        {
            xmap = ClientSize.Width / 3919706.0;
            ymap = ClientSize.Height / 3194334.0;
            map = new Bitmap(ClientSize.Width, ClientSize.Height);
            mapbox.Image = map;
            foreach (Point o in orderPoints)
            {
                Point mapped_o = Map2Map(o.X, o.Y);
                //Console.WriteLine($"Writing pixel: {mapped_o.X},{mapped_o.Y}");
                map.SetPixel(mapped_o.X, mapped_o.Y, Color.Red);
            }
            PaintDump();
        }
        double xmap;
        double ymap;
        Point Map2Map(int _x , int _y)
        {
            int x = (int)(xmap * (_x - 55297404));
            x = x >= map.Width ? x - 1 : x;
            int y = (int)(ymap * (_y - 512345518));
            y = y >= map.Height ? y - 1 : y;
            return new Point(x, y);
        }

        void PaintDump()
        {
            Point mapped_dump = Map2Map(dump.X, dump.Y);
            int dx = ClientSize.Width > 1000 ? 3 : 1;
            int dy = ClientSize.Height > 500 ? 3 : 1;
            int x = mapped_dump.X;
            int y = mapped_dump.Y;
            for (int i = x - dx; i <= x + dx; i++)
                for (int j = y - dy; j <= y + dy; j++)
                    map.SetPixel(i, j, Color.Lime);
        }

        public void UpdateMap()
        {
            RouteVisualizer_SizeChanged(null, null);
        }

        private void RouteVisualizer_SizeChanged(object sender, EventArgs e)
        {
            mapbox.Size = new Size(ClientSize.Width, ClientSize.Height);
            //InitializeMap();
            InitializeClusterMap();
        }
    }
}
