﻿using System;
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


        // Тестовая точка.
        Point point;

        // Тестовая линия.
        LineString line;

        // Тестовый полигон.
        Polygon polygon;

        List<(Airplane, Coordinate)> airplanes = new List<(Airplane, Coordinate)>();

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

            const string textFile = "airports.dat";

            // Read file using StreamReader. Reads file line by line   
            var curDir = Directory.GetCurrentDirectory();
            Console.WriteLine(curDir);

            
            //using(var streamReader = new StreamReader("airports.dat")) {
              //  string lines = streamReader.ReadToEnd();
                //Console.WriteLine(lines);
            //}
            
            if (File.Exists(textFile)) {
                var lines = File.ReadAllLines(textFile);
                foreach (var line in lines) {
                    var row = line.Split(",");
                    if (Double.TryParse(row[6], out double result) && Double.TryParse(row[7], out double result2)) {
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


            for (int i = 0; i < 10; i++) {
                AddphpStorm();

            }

            Console.WriteLine("We are planing");
            #region создание кастомного объекта и добавление на карту, модификация полигона заменой точки


            for (int i = 0; i < 50; i++) {
                AddPlane();
            }

            Console.WriteLine(airplanes.Count);
            Console.WriteLine(airportLatLon.Count);


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
            // Двигаем самолет.
            if (airplanes.Count < maximumPlanes) {
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