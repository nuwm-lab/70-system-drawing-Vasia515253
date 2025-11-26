using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;

namespace LabWork
{
    // ====================================================================
    // 1. Структура DataPoint (Інкапсуляція, readonly)
    // ====================================================================
    /// <summary>
    /// Представляє незмінну координату для точки графіка (x, y).
    /// </summary>
    public readonly struct DataPoint
    {
        public double X { get; }
        public double Y { get; }

        public DataPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    // ====================================================================
    // 2. Клас FunctionCalculator (Інкапсуляція математики)
    // ====================================================================
    /// <summary>
    /// Клас для обчислення точок графіка y = (x + cos(2x)) / (x + 2).
    /// </summary>
    public static class FunctionCalculator
    {
        private const double X_START = 0.2;
        private const double X_END = 10.0;
        private const double DELTA_X = 0.8;

        public static double XMin => X_START;
        public static double XMax => X_END;

        /// <summary>
        /// Обчислює значення Y для заданого X.
        /// </summary>
        /// <exception cref="DivideByZeroException">Викидається при x = -2.</exception>
        public static double CalculateY(double x)
        {
            double denominator = x + 2;
            if (Math.Abs(denominator) < 1e-9) // Захист від ділення на нуль
            {
                throw new DivideByZeroException("Ділення на нуль при x = -2.");
            }
            return (x + Math.Cos(2 * x)) / denominator;
        }

        /// <summary>
        /// Генерує список точок для побудови графіка.
        /// </summary>
        public static List<DataPoint> GeneratePoints()
        {
            List<DataPoint> points = new List<DataPoint>();
            for (double x = X_START; x <= X_END + DELTA_X / 2; x += DELTA_X) // + DELTA_X/2 для включення X_END
            {
                try
                {
                    double y = CalculateY(x);
                    points.Add(new DataPoint(x, y));
                }
                catch (DivideByZeroException)
                {
                    // Обробка розривів функції
                    Console.WriteLine($"Розрив функції при x = {x:F2}");
                }
            }
            return points;
        }
    }

    // ====================================================================
    // 3. Клас PlottingForm (UI та логіка рендерингу)
    // ====================================================================
    public class PlottingForm : Form
    {
        private const int PADDING = 40; // Відступ від країв
        private const double Y_PADDING_FACTOR = 0.1; // Запас по Y

        private readonly List<DataPoint> _dataPoints;
        private double _yMin;
        private double _yMax;
        
        // GDI+ об'єкти для багаторазового використання (оптимізація)
        private Pen _plotPen;
        private Brush _pointBrush;
        private Pen _axesPen;

        // Змінна для додаткового завдання: перемикання режимів
        private bool _isLineMode = true; 

        public PlottingForm()
        {
            // Налаштування форми
            this.Text = "Графік функції: y = (x + cos(2x)) / (x + 2)";
            this.Size = new Size(800, 600);
            
            // Ініціалізація GDI+ об'єктів
            _plotPen = new Pen(Color.Blue, 2);
            _pointBrush = Brushes.Red;
            _axesPen = Pens.Gray;

            // Обчислення та визначення діапазону
            _dataPoints = FunctionCalculator.GeneratePoints();
            CalculateYRange();

            // Обробка події Paint (для малювання)
            this.Paint += PlottingForm_Paint;
            // Обробка події Resize (викликає Paint)
            this.Resize += (sender, e) => this.Invalidate();
            
            // Вмикаємо подвійний буфер
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            // Додаємо UI для додаткового завдання (перемикання режимів)
            AddModeSwitchControls();
        }

        /// <summary>
        /// Додає елементи керування для перемикання режиму відображення.
        /// </summary>
        private void AddModeSwitchControls()
        {
            RadioButton rbLine = new RadioButton { Text = "Лінійний", Checked = true, Location = new Point(10, 10), AutoSize = true };
            RadioButton rbPoint = new RadioButton { Text = "Точковий", Checked = false, Location = new Point(10, 30), AutoSize = true };

            rbLine.CheckedChanged += (s, e) => {
                if (rbLine.Checked) 
                {
                    _isLineMode = true;
                    this.Invalidate(); 
                }
            };
            rbPoint.CheckedChanged += (s, e) => {
                if (rbPoint.Checked) 
                {
                    _isLineMode = false;
                    this.Invalidate(); 
                }
            };

            this.Controls.Add(rbLine);
            this.Controls.Add(rbPoint);
        }

        /// <summary>
        /// Визначає мінімальне та максимальне значення Y для коректного масштабування.
        /// </summary>
        private void CalculateYRange()
        {
            if (!_dataPoints.Any()) return;

            _yMin = _dataPoints.Min(p => p.Y);
            _yMax = _dataPoints.Max(p => p.Y);

            // Додаємо запас
            double yRange = _yMax - _yMin;
            if (yRange == 0) yRange = 1.0; // Захист від випадку, коли всі Y однакові
            
            _yMin -= yRange * Y_PADDING_FACTOR;
            _yMax += yRange * Y_PADDING_FACTOR;
        }

        /// <summary>
        /// Основний метод малювання, викликається системою при необхідності.
        /// </summary>
        private void PlottingForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int clientWidth = this.ClientSize.Width;
            int clientHeight = this.ClientSize.Height;

            if (_dataPoints.Count < 2) return;

            // 1. Налаштування масштабування
            double plotWidth = clientWidth - 2 * PADDING;
            double plotHeight = clientHeight - 2 * PADDING;
            
            double xRange = FunctionCalculator.XMax - FunctionCalculator.XMin;
            double yRange = _yMax - _yMin;
            
            double scaleX = plotWidth / xRange;
            double scaleY = plotHeight / yRange;

            // 2. Функції перетворення координат (Mapping math -> screen)
            
            // Перетворює математичну X у піксельну X
            int MapToScreenX(double x)
            {
                return (int)(PADDING + (x - FunctionCalculator.XMin) * scaleX);
            }

            // Перетворює математичну Y у піксельну Y (з урахуванням інверсії Y у пікселях)
            int MapToScreenY(double y)
            {
                return (int)(clientHeight - PADDING - (y - _yMin) * scaleY);
            }
            
            // 3. Малювання осей і сітки (Спрощено)
            DrawAxes(g, clientWidth, clientHeight, MapToScreenX, MapToScreenY);
            
            // 4. Малювання графіка
            DrawGraph(g, MapToScreenX, MapToScreenY);
        }

        /// <summary>
        /// Малює осі координат та мітки.
        /// </summary>
        private void DrawAxes(Graphics g, int clientWidth, int clientHeight, Func<double, int> mapX, Func<double, int> mapY)
        {
            int xAxisY = mapY(0); 
            int yAxisX = mapX(FunctionCalculator.XMin); 
            
            // Лінія X-осі (якщо Y=0 знаходиться в області малювання)
            if (xAxisY >= PADDING && xAxisY <= clientHeight - PADDING)
            {
                g.DrawLine(_axesPen, PADDING, xAxisY, clientWidth - PADDING, xAxisY);
                g.DrawString("X", SystemFonts.DefaultFont, Brushes.Black, clientWidth - PADDING + 5, xAxisY - 10);
            }

            // Лінія Y-осі
            g.DrawLine(_axesPen, yAxisX, PADDING, yAxisX, clientHeight - PADDING);
            g.DrawString("Y", SystemFonts.DefaultFont, Brushes.Black, yAxisX - 15, PADDING - 20);
            
            // Мітка початку координат
            g.DrawString($"{_yMin:F2}", SystemFonts.DefaultFont, Brushes.Gray, (int)PADDING, clientHeight - (int)PADDING + 5);
        }

        /// <summary>
        /// Малює обчислені точки графіка у вибраному режимі.
        /// </summary>
        private void DrawGraph(Graphics g, Func<double, int> mapX, Func<double, int> mapY)
        {
            Point currentPoint;
            Point previousPoint = Point.Empty;
            
            // Розмір маркера
            const int markerSize = 6; 

            for (int i = 0; i < _dataPoints.Count; i++)
            {
                currentPoint = new Point(mapX(_dataPoints[i].X), mapY(_dataPoints[i].Y));
                
                if (i > 0)
                {
                    // Режим: Лінійний
                    if (_isLineMode)
                    {
                        g.DrawLine(_plotPen, previousPoint, currentPoint);
                    }
                }
                
                // Режим: Точковий або Лінійний (точки завжди малюємо)
                g.FillEllipse(_pointBrush, currentPoint.X - markerSize / 2, currentPoint.Y - markerSize / 2, markerSize, markerSize);
                
                previousPoint = currentPoint;
            }
        }

        /// <summary>
        /// Коректне звільнення GDI+ об'єктів.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _plotPen?.Dispose();
                _axesPen?.Dispose();
                // Brushes.Red — системний об'єкт, не потребує Dispose, тому _pointBrush можна не звільняти, 
                // якщо він ініціалізований системним Brushes. Якщо б ми створювали new SolidBrush(...), Dispose був би потрібен.
            }
            base.Dispose(disposing);
        }
    }
    
    // ====================================================================
    // 4. Головна програма (Запуск WinForms)
    // ====================================================================
    class Program
    {
        [STAThread] 
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Запуск форми
            Application.Run(new PlottingForm());
        }
    }
}
