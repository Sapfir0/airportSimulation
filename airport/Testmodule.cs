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

namespace TestModule {


    public class airport : OSMLSModule {
        public (int leftX, int rightX, int downY, int upY) map;

        // Тестовая точка.
        Point point;

        // Тестовая линия.
        LineString line;

        // Тестовый полигон.
        Polygon polygon;

        List<(Airplane, Coordinate)> airplanes = new List<(Airplane, Coordinate)>(); 

        List<(int, int)> airportCoordinates = new List<(int, int)>();
        List<Coordinate[]> storms = new List<Coordinate[]>();
        const int maximumPlanes = 1000;
        const int countAirport = 40;

        class RealAirports {
            string country;
            double latitude;
            double longitude;
        }

        protected override void Initialize() {
            var rand = new Random();


            for (int i=0; i<10; i++) {
                AddphpStorm();
 
            }


            #region создание кастомного объекта и добавление на карту, модификация полигона заменой точки

            for (var i = 0; i < countAirport; i++) {
                var lat = rand.Next(-70, 70);
                var lan = rand.Next(-70, 70);

                var airportCoordinate = MathExtensions.LatLonToSpherMerc(lat, lan);
                MapObjects.Add(new Airport(airportCoordinate, 0));
                airportCoordinates.Add((lat, lan));
            }

            for(int i=0; i< 50; i++) {
                AddPlane();
            }

            Console.WriteLine(airplanes.Count);
            
            #endregion
        }

        public void AddPlane() {
            var sourceAirport = airportCoordinates[new Random().Next(airportCoordinates.Count)];
            var destAirport = airportCoordinates[new Random().Next(airportCoordinates.Count)];

            while (destAirport == sourceAirport) {
                destAirport = airportCoordinates[new Random().Next(airportCoordinates.Count)];
            }

            var airportCoordinate = MathExtensions.LatLonToSpherMerc(sourceAirport.Item1, sourceAirport.Item2);
            var destAirportCoordinate = MathExtensions.LatLonToSpherMerc(destAirport.Item1, destAirport.Item2);

            var airplane = new Airplane(airportCoordinate, 10000);

            airplanes.Add((airplane, destAirportCoordinate));
            MapObjects.Add(airplane);
        }

        /// <summary>
        /// Вызывается постоянно, здесь можно реализовывать логику перемещений и всего остального, требующего времени.
        /// </summary>
        /// <param name="elapsedMilliseconds">TimeNow.ElapsedMilliseconds</param>
        public override void Update(long elapsedMilliseconds) {
            // Двигаем самолет.
            if(airplanes.Count < maximumPlanes) {
                AddPlane();
            }

            foreach (var airplane in airplanes) {
                var realPlane = airplane.Item1;
                var realDest = airplane.Item2;

                var foo = $"{realPlane.X} {realPlane.Y} летим к {realDest.X} {realDest.Y}";
                //Console.WriteLine(foo);
                realPlane.FlyToAirport(realDest, storms);
            }
        }
        public void AddphpStorm() {
            var rand = new Random();

            var lat = rand.Next(-70, 70);
            var lan = rand.Next(-70, 70);
            var airportCoordinate = MathExtensions.LatLonToSpherMerc(lat, lan);


            var polygonCoordinates = new Coordinate[] {
                    airportCoordinate,
                    MathExtensions.LatLonToSpherMerc(lat+5, lan+1),
                    MathExtensions.LatLonToSpherMerc(lat+3, lan-2),
                    MathExtensions.LatLonToSpherMerc(lat+2, lan-3),
                    airportCoordinate,
                };
            // Создание стандартного полигона по ранее созданным координатам.
            polygon = new Polygon(new LinearRing(polygonCoordinates));
            MapObjects.Add(polygon);
            storms.Add(polygonCoordinates);
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
                radius: 3,
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
        internal void FlyToAirport(Coordinate mymap, List<Coordinate[]> storms) {
            double eps = 2 * Speed;

            

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
            if (coordinate.Y == mymap.Y && coordinate.X == mymap.X) {
                
            }

        }
    }



    /// <summary>
    /// Самолет, умеющий летать вверх-вправо с заданной скоростью.
    /// </summary>
    [CustomStyle(
        @"new ol.style.Style({
            image: new ol.style.Circle({
                opacity: 1.0,
                scale: 1.0,
                radius: 6,
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