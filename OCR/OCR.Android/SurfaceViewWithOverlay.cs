using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Com.Abbyy.Mobile.Rtr;

namespace OCR.Droid
{
    public class SurfaceViewWithOverlay: SurfaceView
    {
        private Point[] quads;
        private string[] lines;
        private Rect areaOfInterest;
        private int stability;
        private int scaleNominatorX = 1;
        private int scaleDenominatorX = 1;
        private int scaleNominatorY = 1;
        private int scaleDenominatorY = 1;
        private Paint textPaint;
        private Paint lineBoundariesPaint;
        private Paint backgroundPaint;
        private Paint areaOfInterestPaint;

        public SurfaceViewWithOverlay(Context context) : base(context)
        {
            this.SetWillNotDraw(false);

            lineBoundariesPaint = new Paint();
            lineBoundariesPaint.SetStyle(Paint.Style.Stroke);
            lineBoundariesPaint.SetARGB(255, 128, 128, 128);
            textPaint = new Paint();
            areaOfInterestPaint = new Paint();
            areaOfInterestPaint.SetARGB(100, 0, 0, 0);
            areaOfInterestPaint.SetStyle(Paint.Style.Fill);
        }

        public void setScaleX(int nominator, int denominator)
        {
            scaleNominatorX = nominator;
            scaleDenominatorX = denominator;
        }

        public void setScaleY(int nominator, int denominator)
        {
            scaleNominatorY = nominator;
            scaleDenominatorY = denominator;
        }

        public void setFillBackground(Boolean newValue)
        {
            if (newValue)
            {
                backgroundPaint = new Paint();
                backgroundPaint.SetStyle(Paint.Style.Fill);
                backgroundPaint.SetARGB(100, 255, 255, 255);
            }
            else
            {
                backgroundPaint = null;
            }
            Invalidate();
        }

        public void setAreaOfInterest(Rect newValue)
        {
            areaOfInterest = newValue;
            Invalidate();
        }

        public Rect getAreaOfInterest()
        {
            return areaOfInterest;
        }

        public void setLines(TextCaptureServiceTextLine[] lines,
            RecognitionServiceResultStabilityStatus resultStatus)
        {
            if (lines != null && scaleDenominatorX > 0 && scaleDenominatorY > 0)
            {
                this.quads = new Point[lines.Length * 4];
                this.lines = new String[lines.Length];
                for (int i = 0; i < lines.Length; i++)
                {
                    TextCaptureServiceTextLine line = lines[i];
                    for (int j = 0; j < 4; j++)
                    {
                        this.quads[4 * i + j] = new Point(
                            (scaleNominatorX * line.Quadrangle[j].X) / scaleDenominatorX,
                            (scaleNominatorY * line.Quadrangle[j].Y) / scaleDenominatorY
                        );
                    }
                    this.lines[i] = line.Text;
                }
                switch (resultStatus.ToString())
                {
                    case "NotReady":
                        textPaint.SetARGB(255, 128, 0, 0);
                        break;
                    case "Tentative":
                        textPaint.SetARGB(255, 128, 0, 0);
                        break;
                    case "Verified":
                        textPaint.SetARGB(255, 128, 64, 0);
                        break;
                    case "Available":
                        textPaint.SetARGB(255, 128, 128, 0);
                        break;
                    case "TentativelyStable":
                        textPaint.SetARGB(255, 64, 128, 0);
                        break;
                    case "Stable":
                        textPaint.SetARGB(255, 0, 128, 0);
                        break;
                }
                stability = resultStatus.Ordinal();

            }
            else
            {
                stability = 0;
                this.lines = null;
                this.quads = null;
            }
            this.Invalidate();
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            int width = canvas.Width;
            int height = canvas.Height;
            canvas.Save();
            // If there is any result
            if (lines != null)
            {
                // Shade (whiten) the background when stable
                if (backgroundPaint != null)
                {
                    canvas.DrawRect(0, 0, width, height, backgroundPaint);
                }
            }
            if (areaOfInterest != null)
            {
                // Shading and clipping the area of interest
                int left = (areaOfInterest.Left * scaleNominatorX) / scaleDenominatorX;
                int right = (areaOfInterest.Right * scaleNominatorX) / scaleDenominatorX;
                int top = (areaOfInterest.Top * scaleNominatorY) / scaleDenominatorY;
                int bottom = (areaOfInterest.Bottom * scaleNominatorY) / scaleDenominatorY;
                canvas.DrawRect(0, 0, width, top, areaOfInterestPaint);
                canvas.DrawRect(0, bottom, width, height, areaOfInterestPaint);
                canvas.DrawRect(0, top, left, bottom, areaOfInterestPaint);
                canvas.DrawRect(right, top, width, bottom, areaOfInterestPaint);
                canvas.DrawRect(left, top, right, bottom, lineBoundariesPaint);
                canvas.ClipRect(left, top, right, bottom);
            }
            // If there is any result
            if (lines != null)
            {
                // Draw the text lines
                for (int i = 0; i < lines.Length; i++)
                {
                    // The boundaries
                    int j = 4 * i;
                    Path path = new Path();
                    Point p = quads[j + 0];
                    path.MoveTo(p.X, p.Y);
                    p = quads[j + 1];
                    path.LineTo(p.X, p.Y);
                    p = quads[j + 2];
                    path.LineTo(p.X, p.Y);
                    p = quads[j + 3];
                    path.LineTo(p.X, p.Y);
                    path.Close();
                    canvas.DrawPath(path, lineBoundariesPaint);

                    // The skewed text (drawn by coordinate transform)
                    canvas.Save();
                    Point p0 = quads[j + 0];
                    Point p1 = quads[j + 1];
                    Point p3 = quads[j + 3];

                    int dx1 = p1.X - p0.X;
                    int dy1 = p1.Y - p0.Y;
                    int dx2 = p3.X - p0.X;
                    int dy2 = p3.Y - p0.Y;

                    int sqrLength1 = dx1 * dx1 + dy1 * dy1;
                    int sqrLength2 = dx2 * dx2 + dy2 * dy2;

                    double angle = 180 * Math.Atan2(dy2, dx2) / Math.PI;
                    double xskew = (dx1 * dx2 + dy1 * dy2) / Math.Sqrt(sqrLength2);
                    double yskew = Math.Sqrt(sqrLength1 - xskew * xskew);

                    textPaint.TextSize = (float)yskew;
                    String line = lines[i];
                    Rect textBounds = new Rect();
                    textPaint.GetTextBounds(lines[i], 0, line.Length, textBounds);
                    double xscale = Math.Sqrt(sqrLength2) / textBounds.Width();

                    canvas.Translate(p0.X, p0.Y);
                    canvas.Rotate((float)angle);
                    canvas.Skew(-(float)(xskew / yskew), 0.0f);
                    canvas.Scale((float)xscale, 1.0f);

                    canvas.DrawText(lines[i], 0, 0, textPaint);
                    canvas.Restore();
                }
            }
            canvas.Restore();

            // Draw the 'progress'
            if (stability > 0)
            {
                int r = width / 50;
                int y = height - 175 - 2 * r;
                for (int i = 0; i < stability; i++)
                {
                    int x = width / 2 + 3 * r * (i - 2);
                    canvas.DrawCircle(x, y, r, textPaint);
                }
            }
        }
    }
}