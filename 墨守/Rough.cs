using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MoShou
{
    // 手绘线条引擎。算法取自 rough.js（Excalidraw 底层同源）：
    //   直线 → 三次贝塞尔，控制点沿法向按 roughness 随机偏移；
    //   seed 固定 → 形状可复现，不会逐帧抖；
    //   每条线两笔 → 出现笔根，这是手绘感和矢量感的真正分界。
    public static class Rough
    {
        // xorshift32：轻、可复现
        public class Rng
        {
            uint s;
            public Rng(int seed)
            {
                s = (uint)seed;
                if (s == 0) s = 0x9E3779B9u;
            }
            public double Next() // [0,1)
            {
                s ^= s << 13;
                s ^= s >> 17;
                s ^= s << 5;
                return (double)(s & 0xFFFFFFu) / 16777216.0;
            }
            public double Sym() { return Next() * 2.0 - 1.0; } // [-1,1)
        }

        // 单笔直线：起终内缩，中点两侧法向随机
        public static PathGeometry Line(double x1, double y1, double x2, double y2,
                                        double roughness, double bowing, int seed)
        {
            PathGeometry pg = new PathGeometry();
            double dx = x2 - x1, dy = y2 - y1;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.6) return pg;

            Rng r = new Rng(seed);
            double ux = dx / len, uy = dy / len;
            double nx = -uy, ny = ux;

            double inset = Math.Min(1.8, len * 0.05);
            double sx = x1 + ux * inset, sy = y1 + uy * inset;
            double ex = x2 - ux * inset, ey = y2 - uy * inset;

            double mx = (sx + ex) * 0.5, my = (sy + ey) * 0.5;
            double amp = Math.Min(2.4, len * 0.035);

            double bow = bowing * amp * r.Sym() * 0.6;
            double o1 = roughness * amp * r.Sym();
            double o2 = roughness * amp * r.Sym();

            double c1x = mx - ux * len * 0.20 + nx * (o1 + bow);
            double c1y = my - uy * len * 0.20 + ny * (o1 + bow);
            double c2x = mx + ux * len * 0.20 + nx * (o2 + bow);
            double c2y = my + uy * len * 0.20 + ny * (o2 + bow);

            PathFigure f = new PathFigure();
            f.StartPoint = new Point(sx, sy);
            f.Segments.Add(new BezierSegment(
                new Point(c1x, c1y), new Point(c2x, c2y), new Point(ex, ey), true));
            pg.Figures.Add(f);
            return pg;
        }

        // 单笔圆弧：8 段平滑采样 + 三次贝塞尔磨圆 + 轻微法向手绘扰动（不出现折线棱角，绝不出整圆）
        public static PathFigure ArcFig(double cx, double cy, double rad,
                                        double a0, double a1, double roughness, int seed)
        {
            Rng r = new Rng(seed);
            int seg = 8;
            double amp = Math.Min(0.75, rad * 0.065);
            Point[] pts = new Point[seg + 1];

            for (int i = 0; i <= seg; i++)
            {
                double t = (double)i / seg;
                double ang = (a0 + (a1 - a0) * t) * Math.PI / 180.0;
                double px = cx + rad * Math.Cos(ang);
                double py = cy + rad * Math.Sin(ang);

                if (i > 0 && i < seg)
                {
                    double d = roughness * amp * r.Sym();
                    px += Math.Cos(ang) * d;
                    py += Math.Sin(ang) * d;
                }
                pts[i] = new Point(px, py);
            }

            PathFigure f = new PathFigure();
            f.StartPoint = pts[0];
            for (int i = 0; i < seg; i++)
            {
                Point p0 = i > 0 ? pts[i - 1] : pts[i];
                Point p1 = pts[i];
                Point p2 = pts[i + 1];
                Point p3 = (i + 2 <= seg) ? pts[i + 2] : pts[i + 1];
                Point c1 = new Point(p1.X + (p2.X - p0.X) / 6.0, p1.Y + (p2.Y - p0.Y) / 6.0);
                Point c2 = new Point(p2.X - (p3.X - p1.X) / 6.0, p2.Y - (p3.Y - p1.Y) / 6.0);
                f.Segments.Add(new BezierSegment(c1, c2, p2, true));
            }
            return f;
        }

        // 一条格线：主笔 + 副笔，副笔细而淡
        public static void Pen(Canvas host, double x1, double y1, double x2, double y2,
                               Color c, double th, double op,
                               double roughness, double bowing, int seed,
                               List<Shape> sink)
        {
            for (int s = 0; s < 2; s++)
            {
                Path p = new Path();
                p.Data = Line(x1, y1, x2, y2, roughness, bowing, seed + s * 37);
                p.Stroke = new SolidColorBrush(c);
                p.StrokeThickness = s == 0 ? th : th * 0.6;
                p.Opacity = s == 0 ? op : op * 0.5;
                p.IsHitTestVisible = false;
                p.SnapsToDevicePixels = true;
                host.Children.Add(p);
                if (sink != null) sink.Add(p);
            }
        }

        // 四角回纹：四个 90° 弧，四个 Path，主副各一套
        public static void CornerMarks(Canvas host, double w, double h, double m, double R,
                                       Color c, double th, double op, double roughness, int seed,
                                       List<Shape> sink)
        {
            for (int s = 0; s < 2; s++)
            {
                PathGeometry pg = new PathGeometry();
                pg.Figures.Add(ArcFig(m + R, m + R, R, 180, 270, roughness, seed + s * 37));
                pg.Figures.Add(ArcFig(w - m - R, m + R, R, 270, 360, roughness, seed + s * 37 + 1));
                pg.Figures.Add(ArcFig(w - m - R, h - m - R, R, 0, 90, roughness, seed + s * 37 + 2));
                pg.Figures.Add(ArcFig(m + R, h - m - R, R, 90, 180, roughness, seed + s * 37 + 3));

                Path p = new Path();
                p.Data = pg;
                p.Stroke = new SolidColorBrush(c);
                p.StrokeThickness = s == 0 ? th : th * 0.6;
                p.Opacity = s == 0 ? op : op * 0.5;
                p.IsHitTestVisible = false;
                host.Children.Add(p);
                if (sink != null) sink.Add(p);
            }
        }
    }
}
