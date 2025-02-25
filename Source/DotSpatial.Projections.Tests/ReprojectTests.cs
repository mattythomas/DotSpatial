﻿using System;
using NUnit.Framework;

namespace DotSpatial.Projections.Tests
{
    [TestFixture]
    class ReprojectTests
    {
        [Test]
        public void ReprojectPointsWithOtherRanging()
        {
            var geographic = ProjectionInfo.FromProj4String(KnownCoordinateSystems.Geographic.World.WGS1984.ToProj4String());
            var projected = ProjectionInfo.FromProj4String(KnownCoordinateSystems.Projected.World.WebMercator.ToProj4String());
            geographic.Over = true;
            projected.Over = true;
            double[] pointsXY = { -445, 33 };
            double[] pointsXYCopy = { -445, 33 };
            double[] pointsZ = { 0 };
            Reproject.ReprojectPoints(pointsXY, pointsZ, geographic, projected, 0, 1);
            Reproject.ReprojectPoints(pointsXY, pointsZ, projected, geographic, 0, 1);
            Assert.IsTrue(Math.Abs(pointsXY[0] - pointsXYCopy[0]) < 0.00000000001);
            Assert.IsTrue(Math.Abs(pointsXY[1] - pointsXYCopy[1]) < 0.00000000001);
        }

        [Test(Description = "Checks that reprojection for group works the same as reprojection for one point. (https://github.com/DotSpatial/DotSpatial/issues/781)")]
        public void Reprojection_NorthPoleStereographic_ForGroupTheSameAsForOnePoint()
        {
            var wgs = KnownCoordinateSystems.Geographic.World.WGS1984;
            var wgs84Points = new double[] { 10, 10, 45, 45, 80, 80 };

            const double DELTA = double.Epsilon;

            var projectionInfo = KnownCoordinateSystems.Projected.Polar.NorthPoleStereographic;
            var testProjectionPoints = new double[wgs84Points.Length];
            Array.Copy(wgs84Points, testProjectionPoints, testProjectionPoints.Length);
            Reproject.ReprojectPoints(testProjectionPoints, null, wgs, projectionInfo, 0, testProjectionPoints.Length / 2);

            var backWgs84 = new double[testProjectionPoints.Length];
            Array.Copy(testProjectionPoints, backWgs84, backWgs84.Length);

            // Reproject group from projectionInfo to wgs
            Reproject.ReprojectPoints(backWgs84, null, projectionInfo, wgs, 0, backWgs84.Length / 2);

            // Now reproject each point separately and verify that it is same as in group reprojection
            for (int i = 0; i < testProjectionPoints.Length / 2; i++)
            {
                var onePoint = new double[2];
                onePoint[0] = testProjectionPoints[i * 2];
                onePoint[1] = testProjectionPoints[i * 2 + 1];

                Reproject.ReprojectPoints(onePoint, null, projectionInfo, wgs, 0, 1);

                Assert.AreEqual(onePoint[0], backWgs84[i * 2], DELTA);
                Assert.AreEqual(onePoint[1], backWgs84[i * 2 + 1], DELTA);
            }
        }

        [Test(Description = "Checks that there is no NANs in output for LAEA projections (https://github.com/DotSpatial/DotSpatial/issues/387)")]
        public void LAEA_Reprojection_NoNANs()
        {
            var source = ProjectionInfo.FromProj4String("proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs");
            // Any LAEA projection
            var dest = ProjectionInfo.FromProj4String("+proj=laea +lat_0=52 +lon_0=10 +x_0=4321000 +y_0=3210000 +ellps=GRS80 +units=m +datum=WGS84");

            double[] vertices = { 13.5, 51.3 };
            Reproject.ReprojectPoints(vertices, null, source, dest, 0, 1);

            Assert.IsTrue(!double.IsNaN(vertices[0]));
            Assert.IsTrue(!double.IsNaN(vertices[1]));
        }

        [Test(Description = "Verifies OSGB36 reprojection. (https://github.com/DotSpatial/DotSpatial/issues/732")]
        public void OSGB36_Reprojection()
        {
            var sourceProjection = ProjectionInfo.FromAuthorityCode("EPSG", 27700);
            var targetProjection = ProjectionInfo.FromAuthorityCode("EPSG", 4326);
            var xy = new double[] { 465000, 170000 };
            Reproject.ReprojectPoints(xy, null, sourceProjection, targetProjection, 0, 1);

            // see http://www.ordnancesurvey.co.uk/gps/transformation
            const double expectedX = -1.066488;
            const double expectedY = 51.425291;
            const double eps = 0.0001;
            Assert.AreEqual(expectedX, xy[0], eps);
            Assert.AreEqual(expectedY, xy[1], eps);
        }

        [TestCase(3021, 3006, new double[] { 1366152.968, 6851307.390 }, new double[] { 408700, 6847800 }, 0.08)] // tolerance 8 cm
        [TestCase(3006, 3021, new double[] { 408700, 6847800 }, new double[] { 1366152.968, 6851307.390 }, 0.08)] // tolerance 8 cm
        [TestCase(3013, 3006, new double[] { 19061.000, 6851822.032 }, new double[] { 408700, 6847800 }, 1E-3)] // tolerance 1 mm
        [TestCase(3022, 3021, new double[] { 1536875.736, 7037950.238 }, new double[] { 1649079.352, 7041217.283 }, 1E-3)] // tolerance 1 mm
        public void Reproject_Swedish_Projections_Using_AuthorityCodes(int fromEpsgCode, int toEpsgCode, double[] xy, double[] expected, double tolerance)
        {
            var sourceProjection = ProjectionInfo.FromAuthorityCode("EPSG", fromEpsgCode);
            var targetProjection = ProjectionInfo.FromAuthorityCode("EPSG", toEpsgCode);
            Reproject.ReprojectPoints(xy, null, sourceProjection, targetProjection, 0, 1);
            Assert.AreEqual(expected[0], xy[0], tolerance);
            Assert.AreEqual(expected[1], xy[1], tolerance);
        }

        [TestCase(3021, 3006, new double[] { 1366152.968, 6851307.390 }, new double[] { 408700, 6847800 }, 0.08)] // tolerance 8 cm
        [TestCase(3006, 3021, new double[] { 408700, 6847800 }, new double[] { 1366152.968, 6851307.390 }, 0.08)] // tolerance 8 cm
        [TestCase(3013, 3006, new double[] { 19061.000, 6851822.032 }, new double[] { 408700, 6847800 }, 1E-3)] // tolerance 1 mm
        [TestCase(3022, 3021, new double[] { 1536875.736, 7037950.238 }, new double[] { 1649079.352, 7041217.283 }, 1E-3)] // tolerance 1 mm
        public void Reproject_Swedish_Projections_Using_KnownCrsNames(int fromEpsgCode, int toEpsgCode, double[] xy, double[] expected, double tolerance)
        {
            var sourceProjection = getProjectionUsingKnownCrsName(fromEpsgCode);
            var targetProjection = getProjectionUsingKnownCrsName(toEpsgCode);
            Reproject.ReprojectPoints(xy, null, sourceProjection, targetProjection, 0, 1);
            Assert.AreEqual(expected[0], xy[0], tolerance);
            Assert.AreEqual(expected[1], xy[1], tolerance);
        }

        [Test]
        public void RT9025gonV_to_WGS84()
        {
            // Test from https://github.com/DotSpatial/DotSpatial/issues/618
            var target = KnownCoordinateSystems.Projected.NationalGridsSweden.RT9025gonV;
            var dest = KnownCoordinateSystems.Geographic.World.WGS1984;

            var xy = new double[] { 1411545, 6910904 };
            Reproject.ReprojectPoints(xy, null, target, dest, 0, 1);

            Assert.AreEqual(xy[0], 14.10000, 1e-3);
            Assert.AreEqual(xy[1], 62.30000, 1e-3);
        }

        private ProjectionInfo getProjectionUsingKnownCrsName(int epsgCode)
        {
            ProjectionInfo proj = null;
            if (epsgCode == 3021)
                proj = KnownCoordinateSystems.Projected.NationalGridsSweden.RT9025gonV;
            else if (epsgCode == 3022)
                proj = KnownCoordinateSystems.Projected.NationalGridsSweden.RT900gon;
            else if (epsgCode == 3013)
                proj = KnownCoordinateSystems.Projected.NationalGridsSweden.SWEREF991545;
            else if (epsgCode == 3006)
                proj = KnownCoordinateSystems.Projected.NationalGridsSweden.SWEREF99TM;
            else
                throw new Exception("Not included in this test");

            Assert.AreEqual(epsgCode, proj.AuthorityCode);
            return proj;

        }

        [Test]
        public void EuropeanDatum1950UTMZone30N_to_WGS84()
        {
            // Test from https://github.com/DotSpatial/DotSpatial/issues/623

            var ed50 = KnownCoordinateSystems.Projected.UtmOther.EuropeanDatum1950UTMZone30N;
            var wgs84 = KnownCoordinateSystems.Geographic.World.WGS1984;
            double[] xy = { 450306.555, 4480448.5634 };

            Reproject.ReprojectPoints(xy, null, ed50, wgs84, 0, 1);

            Assert.AreEqual(xy[0], -3.5875, 1e-3);
            Assert.AreEqual(xy[1], 40.47136, 1e-3);
        }

        /// <summary>
        /// This tests whether reprojecting a value from JGD2011 to Wgs84 gives the same results as reprojecting the same value from JGD2000 to Wgs84. (Testcase for issue #1262)
        /// </summary>
        /// <param name="i">zone 1 (index 0) to zone 19 (index 18)</param>
        /// <param name="sampleCoord">longitude, latitude of the sample data for the zone</param>
        /// <param name="gsiResult">Ideal convert result confirmed by GSI official Calculation page (https://vldb.gsi.go.jp/sokuchi/surveycalc/surveycalc/bl2xyf.html)</param>
        [Test]
        [TestCase(0, new double[] { 129.51, 33.01 }, new double[] { 1108.9790, 934.3333 })]
        [TestCase(1, new double[] { 131.01, 33.01 }, new double[] { 1108.9790, 934.3333 })]
        [TestCase(2, new double[] { 132.21, 36.01 }, new double[] { 1110.3484, 3906.2093 })]
        [TestCase(3, new double[] { 133.51, 33.01 }, new double[] { 1108.9790, 934.3333 })]
        [TestCase(4, new double[] { 134.41, 36.01 }, new double[] { 1112.1984, 6910.9861 })]
        [TestCase(5, new double[] { 136.01, 36.01 }, new double[] { 1109.5262, 901.4329 })]
        [TestCase(6, new double[] { 137.01, 36.01 }, new double[] { 1120.8316, -14122.4540 })]
        [TestCase(7, new double[] { 138.51, 36.01 }, new double[] { 1109.5262, 901.4329 })]
        [TestCase(8, new double[] { 139.85, 36.01 }, new double[] { 1109.6084, 1502.3881 })]
        [TestCase(9, new double[] { 140.85, 40.01 }, new double[] { 1110.3693, 1422.8810 })]
        [TestCase(10, new double[] { 140.31, 44.01 }, new double[] { 1112.7625, 4811.0821 })]
        [TestCase(11, new double[] { 142.31, 44.01 }, new double[] { 1112.7625, 4811.0821 })]
        [TestCase(12, new double[] { 144.31, 44.01 }, new double[] { 1112.7625, 4811.0821 })]
        [TestCase(13, new double[] { 142.01, 26.01 }, new double[] { 1107.8082, 1000.9923 })]
        [TestCase(14, new double[] { 127.51, 26.01 }, new double[] { 1107.8082, 1000.9923 })]
        [TestCase(15, new double[] { 124.01, 26.01 }, new double[] { 1107.8082, 1000.9923 })]
        [TestCase(16, new double[] { 131.01, 26.01 }, new double[] { 1107.8082, 1000.9923 })]
        [TestCase(17, new double[] { 136.01, 20.01 }, new double[] { 1106.9640, 1046.3001 })]
        [TestCase(18, new double[] { 154.01, 26.01 }, new double[] { 1107.8082, 1000.9923 })]
        public void Jgd2000ReprojectToWgs84EqualsJgd2011ReprojectToWgs84(int i, double[] sampleCoord, double[] gsiResult)
        {
            ProjectionInfo wgs84 = ProjectionInfo.FromEpsgCode(4326);   //WGS84
            ProjectionInfo JGD2000JapanZone = ProjectionInfo.FromEpsgCode(2443 + i);
            ProjectionInfo JGD2011JapanZone = ProjectionInfo.FromEpsgCode(6669 + i);

            double[] z = { 0d };
            double AccuracyLimit = 0.001; // = less than 1mm error

            double[] coord2000 = sampleCoord;
            double[] coord2011 = new double[2] { sampleCoord[0], sampleCoord[1] };

            Reproject.ReprojectPoints(coord2000, z, wgs84, JGD2000JapanZone, 0, 1);
            Reproject.ReprojectPoints(coord2011, z, wgs84, JGD2011JapanZone, 0, 1);

            Assert.AreEqual(gsiResult[1], coord2011[0], AccuracyLimit, "reprojected JGD2011 x value differs from GSI calculation result");
            Assert.AreEqual(gsiResult[0], coord2011[1], AccuracyLimit, "reprojected JGD2011 y value differs from GSI calculation result");

            Assert.AreEqual(gsiResult[1], coord2000[0], AccuracyLimit, "reprojected JGD2000 x value differs from GSI calculation result");
            Assert.AreEqual(gsiResult[0], coord2000[1], AccuracyLimit, "reprojected JGD2000 y value differs from GSI calculation result");
        }
    }
}
