using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new BeachBallForm());
    }
}

public sealed class BeachBallForm : Form
{
    private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer();
    private readonly Random _rng = new Random();

    // Physics
    private PointF _pos;           // Center of the ball
    private PointF _vel;           // Pixels per tick
    private float _radius = 60f;   // Ball radius
    private float _spin = 0f;      // Current spin angle in degrees
    private float _spinVel;        // Degrees per tick

    public BeachBallForm()
    {
        Text = "Beach Ball (C# / WinForms)";
        ClientSize = new Size(900, 550);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(16, 18, 22);

        // Reduce flicker and enable smooth drawing
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

        // Randomize initial state
        _pos = new PointF(ClientSize.Width * 0.25f, ClientSize.Height * 0.33f);
        _vel = RandomVelocity(6f, 8.5f);
        _spinVel = _rng.Next(0, 2) == 0 ? _rng.Next(2, 5) : -_rng.Next(2, 5); // small spin

        // 60 FPS-ish
        _timer.Interval = 16;
        _timer.Tick += (_, __) =>
        {
            StepPhysics();
            Invalidate();
        };
        _timer.Start();

        // Re-center on resize to avoid getting stuck on borders if window shrinks
        Resize += (_, __) =>
        {
            ClampInsideClient();
            Invalidate();
        };
    }

    private PointF RandomVelocity(float min, float max)
    {
        // Random direction with random speed between min and max
        double angle = _rng.NextDouble() * Math.PI * 2;
        float speed = (float)(min + _rng.NextDouble() * (max - min));
        return new PointF((float)(Math.Cos(angle) * speed), (float)(Math.Sin(angle) * speed));
    }

    private void StepPhysics()
    {
        // Move
        _pos.X += _vel.X;
        _pos.Y += _vel.Y;
        _spin = NormalizeAngle(_spin + _spinVel);

        // Bounce
        var left = _radius + 10;   // + margin for nicer look
        var top = _radius + 10;
        var right = ClientSize.Width - _radius - 10;
        var bottom = ClientSize.Height - _radius - 10;

        if (_pos.X < left) { _pos.X = left; _vel.X = Math.Abs(_vel.X); _spinVel = BumpSpin(_spinVel); }
        if (_pos.X > right) { _pos.X = right; _vel.X = -Math.Abs(_vel.X); _spinVel = BumpSpin(_spinVel); }
        if (_pos.Y < top) { _pos.Y = top; _vel.Y = Math.Abs(_vel.Y); _spinVel = BumpSpin(_spinVel); }
        if (_pos.Y > bottom) { _pos.Y = bottom; _vel.Y = -Math.Abs(_vel.Y); _spinVel = BumpSpin(_spinVel); }
    }

    private float BumpSpin(float current)
    {
        // Add a tiny randomized change to simulate frictiony wall hits
        float delta = _rng.Next(-2, 3);
        float next = current + delta;
        // Clamp spin a bit so it doesn’t go wild
        return Math.Max(-9f, Math.Min(9f, next));
    }

    private void ClampInsideClient()
    {
        float left = _radius + 10;
        float top = _radius + 10;
        float right = Math.Max(left, ClientSize.Width - _radius - 10);
        float bottom = Math.Max(top, ClientSize.Height - _radius - 10);
        _pos.X = Math.Max(left, Math.Min(right, _pos.X));
        _pos.Y = Math.Max(top, Math.Min(bottom, _pos.Y));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        // Subtle vignette background
        using (var lg = new LinearGradientBrush(ClientRectangle, Color.FromArgb(24, 26, 32), Color.FromArgb(10, 10, 12), LinearGradientMode.Vertical))
            g.FillRectangle(lg, ClientRectangle);

        DrawBallShadow(g);
        DrawBeachBall(g, _pos, _radius, _spin);
    }

    private void DrawBallShadow(Graphics g)
    {
        // Soft drop shadow under the ball
        float shadowScaleX = 1.25f;
        float shadowScaleY = 0.35f;
        float shadowW = _radius * 2 * shadowScaleX;
        float shadowH = _radius * 2 * shadowScaleY;

        float shadowX = _pos.X - shadowW / 2f;
        float shadowY = _pos.Y + _radius - shadowH / 3f;

        using var path = new GraphicsPath();
        path.AddEllipse(shadowX, shadowY, shadowW, shadowH);

        using var pgb = new PathGradientBrush(path)
        {
            CenterColor = Color.FromArgb(80, 0, 0, 0),
            SurroundColors = new[] { Color.FromArgb(0, 0, 0, 0) }
        };
        g.FillPath(pgb, path);
    }

    private void DrawBeachBall(Graphics g, PointF center, float r, float rotationDeg)
    {
        var rect = new RectangleF(center.X - r, center.Y - r, r * 2, r * 2);

        // Draw a subtle outline
        using (var pen = new Pen(Color.FromArgb(220, 20, 20, 28), 2f))
            g.DrawEllipse(pen, rect);

        // Colored panels (6 wedges). We'll rotate the canvas for spin.
        Color[] wedgeColors =
        {
            Color.FromArgb(248, 84, 84),   // red
            Color.FromArgb(255, 210, 86),  // yellow
            Color.FromArgb(90, 200, 110),  // green
            Color.FromArgb(90, 170, 255),  // blue
            Color.FromArgb(255, 135, 70),  // orange
            Color.FromArgb(175, 120, 235), // purple
        };

        // using (var state = g.Save())
        // {
        //     g.TranslateTransform(center.X, center.Y);
        //     g.RotateTransform(rotationDeg);
        //     g.TranslateTransform(-center.X, -center.Y);

        //     float start = -90f; // start at top
        //     float sweep = 360f / wedgeColors.Length;

        //     for (int i = 0; i < wedgeColors.Length; i++)
        //     {
        //         using var wedgeBrush = new SolidBrush(wedgeColors[i]);
        //         g.FillPie(wedgeBrush, rect, start + i * sweep, sweep);

        //         // White seam between panels
        //         using var seamPen = new Pen(Color.White, Math.Max(1.2f, r * 0.02f));
        //         g.DrawPie(seamPen, rect, start + i * sweep, sweep);
        //     }
        // }

        var state = g.Save(); // Save current transform state

        g.TranslateTransform(center.X, center.Y);
        g.RotateTransform(rotationDeg);
        g.TranslateTransform(-center.X, -center.Y);

        float start = -90f; // start at top
        float sweep = 360f / wedgeColors.Length;

        for (int i = 0; i < wedgeColors.Length; i++)
        {
            using var wedgeBrush = new SolidBrush(wedgeColors[i]);
            g.FillPie(wedgeBrush, rect, start + i * sweep, sweep);

            using var seamPen = new Pen(Color.White, Math.Max(1.2f, r * 0.02f));
            g.DrawPie(seamPen, rect, start + i * sweep, sweep);
        }

        g.Restore(state); // Restore after drawing


        // "Button" (valve) and highlight for gloss
        float buttonR = r * 0.14f;
        var buttonRect = new RectangleF(center.X - buttonR, center.Y - buttonR, buttonR * 2, buttonR * 2);

        using (var pth = new GraphicsPath())
        {
            pth.AddEllipse(buttonRect);
            using var pgb = new PathGradientBrush(pth)
            {
                CenterColor = Color.White,
                SurroundColors = new[] { Color.FromArgb(220, 230, 235) }
            };
            g.FillPath(pgb, pth);
            using var pen = new Pen(Color.FromArgb(180, 200, 210), 1.2f);
            g.DrawEllipse(pen, buttonRect);
        }

        // Specular highlight
        using (var highlight = new GraphicsPath())
        {
            // A small tilted ellipse near top-left to mimic light
            float hw = r * 0.9f;
            float hh = r * 0.5f;
            var hlRect = new RectangleF(center.X - r * 0.65f, center.Y - r * 0.85f, hw, hh);
            highlight.AddEllipse(hlRect);

            using var pgb = new PathGradientBrush(highlight)
            {
                CenterPoint = new PointF(hlRect.Left + hlRect.Width * 0.35f, hlRect.Top + hlRect.Height * 0.35f),
                CenterColor = Color.FromArgb(120, 255, 255, 255),
                SurroundColors = new[] { Color.FromArgb(0, 255, 255, 255) }
            };
            g.FillPath(pgb, highlight);
        }
    }

    private static float NormalizeAngle(float deg)
    {
        while (deg >= 360f) deg -= 360f;
        while (deg < 0f) deg += 360f;
        return deg;
    }
}
