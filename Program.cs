using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;

namespace LabWork
{
    // ====================================================================
    // 1. Структура для зберігання точок графіка
    // ====================================================================
    /// <summary>
    /// Представляє незмінну координату для точки графіка.
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
    // 2. Клас для обчислення точок (Інкапсуляція математичної логіки)
    // ====================================================================
    /// <summary>
    /// Клас для обчислення точок графіка функції y = (x + cos(2x)) / (x + 2).
    /// </summary>
    public static class FunctionCalculator
    {
        // Контрольні параметри, які визначають область графіка
        private const double X_START = 0.2;
        private const double X_END = 10.0;
        private const double DELTA_X = 0.8;

        /// <summary>
        /// Обчислює значення Y для заданого X.
        /// </summary>
        public static double CalculateY(double x)
        {
            // y = (x + cos(2x)) / (x + 2)
            // Ділення на нуль відсутнє у діапазоні X_START >= 0.2
            return (x + Math.Cos(2 * x)) / (x + 2);
        }

        /// <summary>
        /// Генерує список точок для побудови графіка.
        /// </summary>
        public static List<DataPoint> GeneratePoints()
        {
            List<DataPoint> points = new List<DataPoint>();
            for (double x = X_START; x <= X_END; x += DELTA_X)
            {
                double y = CalculateY(x);
                points.Add(new DataPoint(x, y));
            }
            return points;
        }

        /// <summary>
        /// Повертає початкову межу X.
        /// </summary>
        public static double XMin => X_START;

        /// <summary>
        /// Повертає кінцеву межу X.
        /// </summary>
        public static double XMax => X_END;
    }

    // ====================================================================
    // 3. Форма для відображення графіка
    // ====================================================================
    /// <summary>
    /// Форма, яка відображає графік функції та масштабує його при зміні розмірів.
    /// </summary>
    public class PlottingForm : Form
    {
        private const double PADDING = 30; // Відступ від країв форми у пікселях
        private const double Y_PADDING_FACTOR = 0.1; // Запас по Y
        
        private readonly List<DataPoint> _dataPoints;
        private double _yMin;
        private double _yMax;

        public PlottingForm()
        {
            // Налаштування форми
            this.Text = "Графік функції: y = (x + cos(2x)) / (x + 2)";
            this.Size = new Size(800, 600);
            
            // Обчислення точок
            _dataPoints = FunctionCalculator.GeneratePoints();

            // Визначення діапазону Y
            CalculateYRange();

            // Додаємо обробник події Paint, який викликається при перемальовуванні (включно з resize)
            this.Paint += PlottingForm_Paint;
            
            // Вмикаємо подвійний буфер для зменшення мерехтіння при resize
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        /// <summary>
        /// Визначає мінімальне та максимальне значення Y для коректного масштабування.
        /// </summary>
        private void CalculateYRange()
        {
            if (_dataPoints.Count == 0) return;

            _yMin = double.MaxValue;
            _yMax = double.MinValue;

            foreach (var p in _dataPoints)
            {
                if (p.Y < _yMin) _yMin = p.Y;
                if (p.Y > _yMax) _yMax = p.Y;
            }

            // Додаємо запас
            double yRange = _yMax - _yMin;
            _yMin -= yRange * Y_PADDING_FACTOR;
            _yMax += yRange * Y_PADDING_FACTOR;
        }

        /// <summary>
        /// Основний обробник події Paint, який здійснює малювання та масштабування.
        /// </summary>
        private void PlottingForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int clientWidth = this.ClientSize.Width;
            int clientHeight = this.ClientSize.Height;

            if (_dataPoints.Count < 2) return;

            // 1. Налаштування області малювання
            double plotWidth = clientWidth - 2 * PADDING;
            double plotHeight = clientHeight - 2 * PADDING;
            
            // Діапазони
            double xRange = FunctionCalculator.XMax - FunctionCalculator.XMin;
            double yRange = _yMax - _yMin;
            
            // Масштаби
            double scaleX = plotWidth / xRange;
            double scaleY = plotHeight / yRange;

            // 2. Функції перетворення координат (Масштабування)
            
            // Перетворює математичну X-координату у піксельну X-координату на формі
            int ToScreenX(double x)
            {
                return (int)(PADDING + (x - FunctionCalculator.XMin) * scaleX);
            }

            // Перетворює математичну Y-координату у піксельну Y-координату на формі
            // (Y пікселів зростає вниз, тому віднімаємо від верхньої межі області)
            int ToScreenY(double y)
            {
                return (int)(clientHeight - PADDING - (y - _yMin) * scaleY);
            }
            
            // 3. Малювання осей
            
            int yAxisX = ToScreenX(FunctionCalculator.XMin); // Вісь Y на лівій межі X
            int xAxisY = ToScreenY(0); // Вісь X на рівні Y=0 (якщо 0 у діапазоні)
            
            // Лінія X-осі
            g.DrawLine(Pens.Gray, (int)PADDING, xAxisY, clientWidth - (int)PADDING, xAxisY);
            g.DrawString("X", SystemFonts.DefaultFont, Brushes.Black, clientWidth - PADDING + 5, xAxisY - 10);
            
            // Лінія Y-осі
            g.DrawLine(Pens.Gray, yAxisX, (int)PADDING, yAxisX, clientHeight - (int)PADDING);
            g.DrawString("Y", SystemFonts.DefaultFont, Brushes.Black, yAxisX - 15, (int)PADDING - 20);
            
            // Мітка початку координат
            g.DrawString($"({FunctionCalculator.XMin}, {_yMin:F2})", SystemFonts.DefaultFont, Brushes.Gray, (int)PADDING, clientHeight - (int)PADDING + 5);

            // 4. Малювання графіка
            
            Point currentPoint;
            Point previousPoint = Point.Empty;
            
            Pen plotPen = new Pen(Color.Blue, 2);

            for (int i = 0; i < _dataPoints.Count; i++)
            {
                currentPoint = new Point(ToScreenX(_dataPoints[i].X), ToScreenY(_dataPoints[i].Y));
                
                // Малюємо лінію між точками
                if (i > 0)
                {
                    g.DrawLine(plotPen, previousPoint, currentPoint);
                }
                
                previousPoint = currentPoint;

                // Малюємо точки
                g.FillEllipse(Brushes.Red, currentPoint.X - 3, currentPoint.Y - 3, 6, 6);
            }
        }
    }
    
    // ====================================================================
    // 4. Головна програма (Запуск WinForms)
    // ====================================================================
    class Program
    {
        // Атрибут [STAThread] є обов'язковим для Windows Forms
        [STAThread] 
        static void Main(string[] args)
        {
            // Конфігурація середовища Windows Forms
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Запуск форми з графіком
            Application.Run(new PlottingForm());
        }
    }
}
