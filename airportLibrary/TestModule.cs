using System;
using NetTopologySuite.Geometries;
using OSMLSGlobalLibrary;
using OSMLSGlobalLibrary.Map;
using OSMLSGlobalLibrary.Modules;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Operation.Overlay;
using System.Collections;
using System.IO;
using System.Globalization;
using System.Collections.Specialized;
using System.Reflection;

namespace TestModule {


    public class airport : OSMLSModule {
        public (int leftX, int rightX, int downY, int upY) map;

        List<(double lat, double lon)> airportLatLon = new List<(double lat, double lon)>();

        List<(Airplane, Coordinate)> airplanes = new List<(Airplane, Coordinate)>();

        List<Coordinate> storms = new List<Coordinate>();
        const int maximumPlanes = 1000; // в реальной жизни около 11-12тыс

        class RealAirports {
            string country;
            double latitude;
            double longitude;
        }

        protected override void Initialize() {
            var rand = new Random();

            const string textFile = "airports.dat";

            if (File.Exists(textFile)) {
                var lines = File.ReadAllLines(textFile);
                foreach (var line in lines) {
                    var row = line.Split(",");
                    if (double.TryParse(row[6], out double result) && Double.TryParse(row[7], out double result2)) {
                        airportLatLon.Add((Convert.ToDouble(row[6]), Convert.ToDouble(row[7])));
                    } else {
                        Console.WriteLine(row[6] + " " + row[7]);
                    }
                }
            } else {
                 throw new Exception("Data file not found");
             }


            for (int i = 0; i < airportLatLon.Count; i++) {
                var airportCoordinate = MathExtensions.LatLonToSpherMerc(airportLatLon[i].lat, airportLatLon[i].lon);
                MapObjects.Add(new Airport(airportCoordinate, 0));
            }


            for (int i = 0; i < 20; i++) {
                AddStorm();

            }

            #region создание кастомного объекта и добавление на карту, модификация полигона заменой точки

            for (int i = 0; i < 50; i++) {
                AddPlane();
            }


            #endregion
        }

        public void AddPlane() {
            var sourceAirport = airportLatLon[new Random().Next(airportLatLon.Count)];
            var destAirport = airportLatLon[new Random().Next(airportLatLon.Count)];

            while (destAirport == sourceAirport) {
                destAirport = airportLatLon[new Random().Next(airportLatLon.Count)];
            }

            var airportCoordinate = MathExtensions.LatLonToSpherMerc(sourceAirport.Item1, sourceAirport.Item2);
            var destAirportCoordinate = MathExtensions.LatLonToSpherMerc(destAirport.Item1, destAirport.Item2);

            var airplane = new Airplane(airportCoordinate, 1000);

            airplanes.Add((airplane, destAirportCoordinate));
            MapObjects.Add(airplane);
        }


        /// <summary>
        /// Вызывается постоянно, здесь можно реализовывать логику перемещений и всего остального, требующего времени.
        /// </summary>
        /// <param name="elapsedMilliseconds">TimeNow.ElapsedMilliseconds</param>
        public override void Update(long elapsedMilliseconds) {

            if (airplanes.Count < maximumPlanes) {
                AddPlane();
            }

            var arrivedPlanes = airplanes.Where(plane => (Math.Abs(plane.Item1.X - plane.Item2.X) < plane.Item1.Speed || Math.Abs(plane.Item1.Y - plane.Item2.Y) < plane.Item1.Speed));

            foreach (var plane in arrivedPlanes) {
                MapObjects.Remove(plane.Item1);
                Console.WriteLine("From card was removed " + plane.Item1.X + " " + plane.Item1.Y);
            }

           // не могу удалитб по-другому
            airplanes.RemoveAll(plane => (Math.Abs(plane.Item1.X - plane.Item2.X) < plane.Item1.Speed || Math.Abs(plane.Item1.Y - plane.Item2.Y) < plane.Item1.Speed)); // самолет прибывает на место назначения


            Console.WriteLine(airplanes.Count);

            foreach (var airplane in airplanes) {
                var realPlane = airplane.Item1;
                var realDest = airplane.Item2;
                realPlane.FlyToAirport(realDest, storms);
            }

            foreach(var storm in storms) {

            }
        }

        public void AddStorm() {
            var rand = new Random();
 
            var coord = MathExtensions.LatLonToSpherMerc(rand.Next(-70, 70), rand.Next(-70, 70));
            MapObjects.Add(new Storm(coord, 10));

            storms.Add(coord);
        }
    }

    #region объявления класса, унаследованного от точки, объекты которого будут иметь уникальный стиль отображения на карте

    /// <summary>
    /// Самолет, умеющий летать вверх-вправо с заданной скоростью.
    /// </summary>
    [CustomStyle(
        @"new ol.style.Style({
            image: new ol.style.Circle({
                opacity: 1.0,
                scale: 1.0,
                radius: 2,
                fill: new ol.style.Fill({
                    color: 'rgba(255, 0, 255, 0.4)'
                }),
                stroke: new ol.style.Stroke({
                    color: 'rgba(0, 0, 0, 0.4)',
                    width: 1
                }),
            })
        });
        ")] // Переопределим стиль всех объектов данного класса, сделав самолет фиолетовым, используя атрибут CustomStyle.
    class Airplane : Point // Унаследуем данный данный класс от стандартной точки.
    {
        /// <summary>
        /// Скорость самолета.
        /// </summary>
        public double Speed { get; }
        public Coordinate coordinate;

        /// <summary>
        /// Конструктор для создания нового объекта.
        /// </summary>
        /// <param name="coordinate">Начальные координаты.</param>
        /// <param name="speed">Скорость.</param>
        public Airplane(Coordinate coordinate, double speed) : base(coordinate) {
            Speed = speed;
            this.coordinate = coordinate;
        }

        /// <summary>
        /// Двигает самолет вверх-вправо.
        /// </summary>
        internal void FlyToAirport(Coordinate mymap, List<Coordinate> storms) {
            double eps = 2 * Speed;

            // Create pen.
           /* Pen blackPen = new Pen(Color.Black, 3);

            // Create points for curve.
            PointF start = new PointF(100.0F, 100.0F);
            PointF control1 = new PointF(200.0F, 10.0F);
            PointF control2 = new PointF(350.0F, 50.0F);
            PointF end = new PointF(500.0F, 100.0F);

            // Draw arc to screen.
            e.Graphics.DrawBezier(blackPen, start, control1, control2, end);
            */


            if (coordinate.X < mymap.X) {
                X += Speed;
            }
            if (coordinate.X > mymap.X) {
                X -= Speed;
            }
            if (coordinate.Y < mymap.Y) {
                Y += Speed;
            }
            if (coordinate.Y > mymap.Y) {
                Y -= Speed;
            }

            

        }
    }

    /// <summary>
    /// Тучка
    /// </summary>
    [CustomStyle(
        @"new ol.style.Style({
            image: new ol.style.Circle({
                opacity: 1.0,
                scale: 1.0,
                radius: 20,
                fill: new ol.style.Fill({
                    color: 'rgba(0, 125, 125, 0.4)'
                }),
                stroke: new ol.style.Stroke({
                    color: 'rgba(0, 0, 0, 0.4)',
                    width: 1
                }),
            })
        });
        ")]
    class Storm : Point {
        public Coordinate coordinate;
        public double Speed { get; }


        public Storm(Coordinate coordinate, double speed) : base(coordinate) {
            this.coordinate = coordinate;
            Speed = speed;

        }
    }

    /// <summary>
    /// Аэропорт
    /// </summary>
    [CustomStyle(
        @"new ol.style.Style({
            image: new ol.style.Circle({
                opacity: 1.0,
                scale: 1.0,
                radius: 3,
                fill: new ol.style.Fill({
                    color: 'rgba(0, 0, 255, 0.4)'
                }),
                stroke: new ol.style.Stroke({
                    color: 'rgba(0, 0, 0, 0.4)',
                    width: 1
                }),
            })
        });
        ")]
    class Airport : Point {
        public Coordinate coordinate;
        public double Speed { get; }


        public Airport(Coordinate coordinate, double speed) : base(coordinate) {
            this.coordinate = coordinate;
            Speed = speed;

        }
    }

    #endregion
}