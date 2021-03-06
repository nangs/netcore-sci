using System;
using System.Linq;
using static System.Math;
using System.Collections.Generic;
using SearchAThing.Sci;
using netDxf.Entities;
using SearchAThing.Util;

namespace SearchAThing
{

    namespace Sci
    {

        public class Circle3D : Arc3D
        {

            public Circle3D(CoordinateSystem3D cs, double r) : base(cs, r, 0, 2 * PI)
            {
                Type = GeometryType.Circle3D;
            }

            /// <summary>
            /// Build 3d circle that intersect p1,p2,p3
            /// ( the inside CS will centered in the circle center and Xaxis toward p1 )
            /// </summary>        
            public Circle3D(Vector3D p1, Vector3D p2, Vector3D p3) : base(p1, p2, p3)
            {
                Type = GeometryType.Circle3D;
            }

            public override EntityObject DxfEntity
            {
                get
                {
                    var c = new netDxf.Entities.Circle(CS.Origin, Radius);
                    c.Normal = CS.BaseZ;
                    return c;
                }
            }

            /// <summary>
            /// creates a polygon approximation of this circle with segments of given maxLength
            /// </summary>            
            public IEnumerable<Vector3D> ToPolygon3D(double segLenMax)
            {
                var alpha_step = segLenMax / Radius;
                var alpha = 0.0;
                var alpha_stop = 2 * PI;

                var origPt = new Vector3D(Radius, 0);
                Vector3D prevPt = origPt;

                yield return origPt.ToWCS(CS);

                alpha += alpha_step;

                while (alpha < alpha_stop)
                {
                    var nextPt = origPt.RotateAboutZAxis(alpha);

                    yield return nextPt.ToWCS(CS);

                    prevPt = nextPt;

                    alpha += alpha_step;
                }
            }

            /// <summary>
            /// build 3d circle that tangent to lines t1,t2 and that intersects point p
            /// note: point p must contained in one of t1,t2
            /// circle will be inside region t1.V toward t2.V
            /// they are 4 circles
            /// </summary>            
            public static IEnumerable<Circle3D> CirclesTan12P(double tol_len, Line3D t1, Line3D t2, Vector3D p)
            {
                foreach (var da in new double[] { 0, PI / 2 })
                {
                    var ip = t1.Intersect(tol_len, t2);
                    var angle = t1.V.AngleRad(tol_len, t2.V);
                    var t3 = new Line3D(ip, t1.V.RotateAs(tol_len, t1.V, t2.V, .5, da), Line3DConstructMode.PointAndVector);

                    Line3D lp = null;
                    Line3D lNp = null;
                    if (t1.LineContainsPoint(tol_len, p)) { lp = t1; lNp = t2; }
                    else if (t2.LineContainsPoint(tol_len, p)) { lp = t2; lNp = t1; }
                    else throw new Exception($"circle 2 tan 1 point : pt must contained in one of given tan");

                    var lpp = new Line3D(p, lp.V.RotateAboutAxis(t1.V.CrossProduct(t2.V), PI / 2), Line3DConstructMode.PointAndVector);
                    var c = lpp.Intersect(tol_len, t3);

                    var Radius = p.Distance(c);
                    var CS = new CoordinateSystem3D(c, lpp.V, t2.V);

                    yield return new Circle3D(CS, Radius);

                    // mirrored addictional circle

                    var mc = c.Mirror(new Line3D(p, p.Project(lNp) - p, Line3DConstructMode.PointAndVector));

                    yield return new Circle3D(new CoordinateSystem3D(mc, lpp.V, t2.V), Radius);
                }
            }

            /// <summary>
            /// build 3d circle through point p, tangent to given t line, with given radius r            
            /// they can be two
            /// </summary>            
            public static IEnumerable<Circle3D> CircleRTanP(double tol_len, Vector3D p, Line3D t, double r)
            {
                var pp = p.Project(t);
                var alpha = Asin((r - pp.Distance(p)) / r);
                var beta = PI / 2 - alpha;

                var axisz = (p - pp).CrossProduct(t.V);

                var t2 = new Line3D(p, t.V.RotateAboutAxis(axisz, beta), Line3DConstructMode.PointAndVector);

                return CirclesTan12P(tol_len, t, t2, p).Where(w => w.Radius.EqualsTol(tol_len, r));
            }

            public double Area { get { return PI * Radius * Radius; } }
            public override double Length { get { return 2 * PI * Radius; } }

        }
    }

    public static partial class Extensions
    {

        public static Circle3D CircleBy3Points(this IEnumerable<Vector3D> _pts)
        {
            var pts = _pts.ToArray();
            if (pts.Length != 3) throw new Exception("expected 3 points for circle3d");

            return new Circle3D(pts[0], pts[1], pts[2]);
        }

        public static Circle3D ToCircle3D(this netDxf.Entities.Circle dxf_circle)
        {
            return new Circle3D(new CoordinateSystem3D(dxf_circle.Center, dxf_circle.Normal, CoordinateSystem3DAutoEnum.AAA), dxf_circle.Radius);
        }

    }

}
