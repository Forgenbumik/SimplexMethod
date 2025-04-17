using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using static System.Net.WebRequestMethods;
using System.Xml.Linq;
using System.Text;

namespace SimplexMethod
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TextBox[] CelFuncTB;
        TextBox[] FondRabTB;
        TextBox[,] ResMatrixTB;
        int[,] ResMatrix;
        int[] CelFunc;
        int[] FondRab;
        public MainWindow()
        {
            InitializeComponent();
            Rows.SelectedIndex = 0;
            Columns.SelectedIndex = 0;
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            Matrix.RowDefinitions.Clear();
            Matrix.ColumnDefinitions.Clear();
            Matrix.Children.Clear();
        }

        private void GetAnswer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int rows = ResMatrixTB.GetLength(0);
                int cols = ResMatrixTB.GetLength(1);

                double[,] constraints = new double[rows, cols];
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        if (i == rows -1 && j == cols - 1)
                        {
                            constraints[i, j] = 0;
                            continue;
                        };
                        constraints[i, j] = double.Parse(ResMatrixTB[i, j].Text);
                    }
                }

                double[] b = new double[rows - 1];
                for (int i = 0; i < rows - 1; i++)
                {
                    b[i] = double.Parse(FondRabTB[i].Text);
                }

                double[] c = new double[cols - 1];
                for (int i = 0; i < cols - 1; i++)
                {
                    c[i] = double.Parse(CelFuncTB[i].Text);
                }

                DisplayInitialSystem(constraints, b, c);

                var result = SolveSimplex(constraints, b, c);

                DisplaySolution(result.Item1, result.Item2);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}");
            }
        }

        private double[] GetRow(double[,] matrix, int rowIndex)
        {
            int cols = matrix.GetLength(1);
            double[] row = new double[cols];

            for (int col = 0; col < cols; col++)
            {
                row[col] = matrix[rowIndex, col];
            }

            return row;
        }

        private void DisplayInitialSystem(double[,] constraints, double[] b, double[] c)
        {
            NachDannie.RowDefinitions.Clear();
            NachDannie.ColumnDefinitions.Clear();
            NachDannie.Children.Clear();

            int rows = constraints.GetLength(0);
            int cols = constraints.GetLength(1);

            ColumnDefinition column = new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            };
            NachDannie.ColumnDefinitions.Add(column);

            for (int i = 0; i < rows + 3; i++)
            {
                RowDefinition row = new RowDefinition
                {
                    Height = new GridLength(1, GridUnitType.Star)
                };
                NachDannie.RowDefinitions.Add(row);
            }

            TextBlock objectiveFunction = new TextBlock
            {
                Text = $"F = {string.Join(" + ", c.Select((val, index) => $"{val}x{index + 1}"))} => max",
                TextAlignment = TextAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold
            };
            Grid.SetRow(objectiveFunction, 0);
            Grid.SetColumn(objectiveFunction, 0);
            NachDannie.Children.Add(objectiveFunction);

            for (int i = 0; i < rows - 1; i++)
            {
                string constraint = string.Join(" + ", GetRow(constraints, i)
                    .Select((val, index) => $"{val}x{index + 1}")) + $" <= {b[i]}";

                TextBlock constraintRow = new TextBlock
                {
                    Text = constraint,
                    TextAlignment = TextAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(constraintRow, i + 1);
                Grid.SetColumn(constraintRow, 0);
                NachDannie.Children.Add(constraintRow);
            }

            TextBlock nonNegativity = new TextBlock
            {
                Text = string.Join(", ", Enumerable.Range(1, cols).Select(i => $"x{i} >= 0")),
                TextAlignment = TextAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold
            };
            Grid.SetRow(nonNegativity, rows + 1);
            Grid.SetColumn(nonNegativity, 0);
            NachDannie.Children.Add(nonNegativity);
        }

        private void DisplaySolution(double[] solution, double maxProfit)
        {
            Answer.RowDefinitions.Clear();
            Answer.ColumnDefinitions.Clear();
            Answer.Children.Clear();

            ColumnDefinition column = new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            };
            Answer.ColumnDefinitions.Add(column);

            for (int i = 0; i <= solution.Length + 1; i++) 
            {
                RowDefinition row = new RowDefinition
                {
                    Height = new GridLength(1, GridUnitType.Star)
                };
                Answer.RowDefinitions.Add(row);
            }

            for (int i = 0; i < solution.Length; i++)
            {
                TextBlock variableCell = new TextBlock
                {
                    Text = $"x{i + 1} = {solution[i]:F2}",
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(variableCell, i);
                Grid.SetColumn(variableCell, 0);
                Answer.Children.Add(variableCell);
            }

            TextBlock profitCell = new TextBlock
            {
                Text = $"Max Profit = {maxProfit:F2}",
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush(Colors.LightGreen)
            };
            Grid.SetRow(profitCell, solution.Length);
            Grid.SetColumn(profitCell, 0);
            Answer.Children.Add(profitCell);
        }

        private Tuple<double[], double> SolveSimplex(double[,] constraints, double[] b, double[] c)
        {
            int rows = constraints.GetLength(0);
            int cols = constraints.GetLength(1);

            double[,] table = new double[rows + 1, cols + rows + 1];
            for (int i = 0; i < rows - 1; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    table[i, j] = constraints[i, j];
                }
                table[i, cols + i] = 1;
                table[i, cols + rows] = b[i];
            }

            for (int j = 0; j < cols - 1; j++)
            {
                table[rows, j] = -c[j];
            }

            while (true)
            {
                int pivotCol = -1;
                double minValue = 0;
                for (int j = 0; j < cols + rows; j++)
                {
                    if (table[rows, j] < minValue)
                    {
                        minValue = table[rows, j];
                        pivotCol = j;
                    }
                }

                if (pivotCol == -1)
                {
                    break;
                }

                int pivotRow = -1;
                double minRatio = double.MaxValue;
                for (int i = 0; i < rows; i++)
                {
                    if (table[i, pivotCol] > 0)
                    {
                        double ratio = table[i, cols + rows] / table[i, pivotCol];
                        if (ratio < minRatio)
                        {
                            minRatio = ratio;
                            pivotRow = i;
                        }
                    }
                }

                if (pivotRow == -1)
                {
                    throw new Exception("Задача не имеет конечного решения.");
                }

                double pivotValue = table[pivotRow, pivotCol];
                for (int j = 0; j < cols + rows + 1; j++)
                {
                    table[pivotRow, j] /= pivotValue;
                }

                for (int i = 0; i < rows + 1; i++)
                {
                    if (i != pivotRow)
                    {
                        double factor = table[i, pivotCol];
                        for (int j = 0; j < cols + rows + 1; j++)
                        {
                            table[i, j] -= factor * table[pivotRow, j];
                        }
                    }
                }
            }

            double[] solution = new double[cols];
            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    if (table[j, i] == 1)
                    {
                        solution[i] = table[j, cols + rows];
                        break;
                    }
                }
            }

            double maxProfit = table[rows, cols + rows];
            return Tuple.Create(solution, maxProfit);
        }

        public void CreateTable(int Row, int Col)
        {
            int Rowo = Row + 2;
            int Colo = Col + 2;

            ResMatrixTB = new TextBox[Row, Col];
            ResMatrix = new int[Row, Col];
            CelFuncTB = new TextBox[Col];
            CelFunc = new int[Col];
            FondRabTB = new TextBox[Row];
            FondRab = new int[Row];

            for (int i = 0; i < Colo; i++)
            {
                ColumnDefinition column = new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                };
                Matrix.ColumnDefinitions.Add(column);
            }

            for (int j = 0; j < Rowo; j++)
            {
                RowDefinition row = new RowDefinition
                {
                    Height = new GridLength(1, GridUnitType.Star)
                };
                Matrix.RowDefinitions.Add(row);
            }

            for (int row = 0; row < Rowo; row++)
            {
                for (int col = 0; col < Colo; col++)
                {
                    TextBox txtbox = new TextBox();

                    if (col == Colo-1 && row == Rowo-1)
                    {
                        continue;
                    }

                    if (col == 0 && row == Rowo-1)
                    {
                        txtbox = new TextBox
                        {
                            IsReadOnly = true,
                            Text = "Целевая функция: ",
                            TextAlignment = TextAlignment.Center,
                            Background = new SolidColorBrush(Colors.Yellow)
                        };
                        Grid.SetRow(txtbox, row);
                        Grid.SetColumn(txtbox, col);
                        Matrix.Children.Add(txtbox);
                    }
                    else if (row == 0 && col == Colo - 1)
                    {
                        txtbox = new TextBox
                        {
                            IsReadOnly = true,
                            Text = "Фонд раб. времени",
                            TextAlignment = TextAlignment.Center,
                            Background = new SolidColorBrush(Colors.Yellow)
                        };
                        Grid.SetRow(txtbox, row);
                        Grid.SetColumn(txtbox, col);
                        Matrix.Children.Add(txtbox);
                    }
                    else if (col == 0 && row == 0)
                    {
                        txtbox = new TextBox
                        {
                            IsReadOnly = true,
                            Background = new SolidColorBrush(Colors.Yellow)
                        };
                        Grid.SetRow(txtbox, row);
                        Grid.SetColumn(txtbox, col);
                        Matrix.Children.Add(txtbox);
                    }
                    else if (col == 0)
                    {
                        txtbox = new TextBox
                        {
                            IsReadOnly = true,
                            Text = $"S{row}",
                            TextAlignment = TextAlignment.Center,
                            Background = new SolidColorBrush(Colors.Yellow)
                        };
                        Grid.SetRow(txtbox, row);
                        Grid.SetColumn(txtbox, col);
                        Matrix.Children.Add(txtbox);
                    }
                    else if (row == 0)
                    {
                        txtbox = new TextBox
                        {
                            IsReadOnly = true,
                            Text = $"x{col}",
                            TextAlignment = TextAlignment.Center,
                            Background = new SolidColorBrush(Colors.Yellow)
                        };
                        Grid.SetRow(txtbox, row);
                        Grid.SetColumn(txtbox, col);
                        Matrix.Children.Add(txtbox);
                    }
                    else
                    {
                        txtbox = new TextBox();
                        Grid.SetRow(txtbox, row);
                        Grid.SetColumn(txtbox, col);
                        Matrix.Children.Add(txtbox);
                    }

                    if (row > 0 && col > 0 && row <= Row && col <= Col)
                    {
                        ResMatrixTB[row - 1, col - 1] = txtbox;
                    }

                    if (col > 0 && row == Rowo - 1 && col <= Col)
                    {
                        CelFuncTB[col - 1] = txtbox;
                    }

                    if (row > 0 && col == Colo - 1 && row <= Row)
                    {
                        FondRabTB[row - 1] = txtbox;
                    }

                }
            }
        }

        public void CreateTable(int[,] elements)
        {
            int Row = elements.GetUpperBound(0) + 1;

            int Col = elements.Length / Row;

            int Rowo = Row + 1;
            int Colo = Col + 1;

            ResMatrixTB = new TextBox[Row, Col];
            ResMatrix = new int[Row, Col];
            CelFuncTB = new TextBox[Col];
            CelFunc = new int[Col];
            FondRabTB = new TextBox[Row];
            FondRab = new int[Row];

            for (int i = 0; i < Colo; i++)
            {
                ColumnDefinition column = new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                };
                Matrix.ColumnDefinitions.Add(column);
            }

            for (int j = 0; j < Rowo; j++)
            {
                RowDefinition row = new RowDefinition
                {
                    Height = new GridLength(1, GridUnitType.Star)
                };
                Matrix.RowDefinitions.Add(row);
            }

            for (int row = 0; row < Rowo; row++)
            {
                for (int col = 0; col < Colo; col++)
                {
                    TextBox txtbox = new TextBox();

                    if (col == Colo - 1 && row == Rowo - 1)
                    {
                        continue;
                    }

                    if (col == 0 && row == Rowo - 1)
                    {
                        txtbox = new TextBox
                        {
                            IsReadOnly = true,
                            Text = "Целевая функция: ",
                            TextAlignment = TextAlignment.Center,
                            Background = new SolidColorBrush(Colors.Yellow)
                        };
                        Grid.SetRow(txtbox, row);
                        Grid.SetColumn(txtbox, col);
                        Matrix.Children.Add(txtbox);
                    }
                    else if (row == 0 && col == Colo - 1)
                    {
                        txtbox = new TextBox
                        {
                            IsReadOnly = true,
                            Text = "Фонд раб. времени",
                            TextAlignment = TextAlignment.Center,
                            Background = new SolidColorBrush(Colors.Yellow)
                        };
                        Grid.SetRow(txtbox, row);
                        Grid.SetColumn(txtbox, col);
                        Matrix.Children.Add(txtbox);
                    }
                    else if (col == 0 && row == 0)
                    {
                        txtbox = new TextBox
                        {
                            IsReadOnly = true,
                            Background = new SolidColorBrush(Colors.Yellow)
                        };
                        Grid.SetRow(txtbox, row);
                        Grid.SetColumn(txtbox, col);
                        Matrix.Children.Add(txtbox);
                    }
                    else if (col == 0)
                    {
                        txtbox = new TextBox
                        {
                            IsReadOnly = true,
                            Text = $"S{row}",
                            TextAlignment = TextAlignment.Center,
                            Background = new SolidColorBrush(Colors.Yellow)
                        };
                        Grid.SetRow(txtbox, row);
                        Grid.SetColumn(txtbox, col);
                        Matrix.Children.Add(txtbox);
                    }
                    else if (row == 0)
                    {
                        txtbox = new TextBox
                        {
                            IsReadOnly = true,
                            Text = $"x{col}",
                            TextAlignment = TextAlignment.Center,
                            Background = new SolidColorBrush(Colors.Yellow)
                        };
                        Grid.SetRow(txtbox, row);
                        Grid.SetColumn(txtbox, col);
                        Matrix.Children.Add(txtbox);
                    }
                    else
                    {
                        txtbox = new TextBox();
                        txtbox.Text = elements[row - 1, col - 1].ToString();
                        Grid.SetRow(txtbox, row);
                        Grid.SetColumn(txtbox, col);
                        Matrix.Children.Add(txtbox);
                    }

                    if (row > 0 && col > 0 && row <= Row && col <= Col)
                    {
                        ResMatrixTB[row - 1, col - 1] = txtbox;
                    }

                    if (col > 0 && row == Rowo - 1 && col <= Col)
                    {
                        CelFuncTB[col - 1] = txtbox;
                    }

                    if (row > 0 && col == Colo - 1 && row <= Row)
                    {
                        FondRabTB[row - 1] = txtbox;
                    }

                }
            }
        }

        private void Columns_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Matrix.RowDefinitions.Clear();
            Matrix.ColumnDefinitions.Clear();
            Matrix.Children.Clear();
            CreateTable(Rows.SelectedIndex + 1, Columns.SelectedIndex + 1);
        }

        private void Rows_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Matrix.RowDefinitions.Clear();
            Matrix.ColumnDefinitions.Clear();
            Matrix.Children.Clear();
            CreateTable(Rows.SelectedIndex + 1, Columns.SelectedIndex + 1);
        }

        private void LoadFromFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Text documents (*.txt)|*.txt|All files (*.*)|*.*";
            dialog.FilterIndex = 2;

            Nullable<bool> result = dialog.ShowDialog();

            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;
                string[] fileText = System.IO.File.ReadAllLines(filename);

                try
                {

                    int[][] jagged = fileText.Select(x => x.Split(' ').Select(int.Parse).ToArray()).ToArray();

                    int rows = jagged.Length;

                    int columns = jagged[0].Length;

                    int[,] constraints = new int[rows, columns];

                    for (int i = 0; i < rows; i++)
                    {
                        for (int j = 0; j < columns; j++)
                        {

                            if (i == rows - 1 && j == columns - 1) continue;
                            constraints[i, j] = jagged[i][j];
                        }
                    }

                    CreateTable(constraints);



                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        private void GetAnswerAndWriteFile(object sender, RoutedEventArgs e)
        {
            try
            {
            int rows = ResMatrixTB.GetLength(0);
            int cols = ResMatrixTB.GetLength(1);

            double[,] constraints = new double[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (i == rows - 1 && j == cols - 1)
                    {
                        constraints[i, j] = 0;
                        continue;
                    }
                    constraints[i, j] = double.Parse(ResMatrixTB[i, j].Text);
                }
            }

            double[] b = new double[rows - 1];
            for (int i = 0; i < rows - 1; i++)
            {
                b[i] = double.Parse(FondRabTB[i].Text);
            }

            double[] c = new double[cols - 1];
            for (int i = 0; i < cols - 1; i++)
            {
                c[i] = double.Parse(CelFuncTB[i].Text);
            }

            DisplayInitialSystem(constraints, b, c);

            string logPath = "solution_log.txt";
            var result = SolveSimplexWithLog(constraints, b, c, logPath);

            DisplaySolution(result.Item1, result.Item2);
            MessageBox.Show($"Решение записано в файл: {logPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}");
            }
        }

        private Tuple<double[], double> SolveSimplexWithLog(double[,] constraints, double[] b, double[] c, string logPath)
        {
            var log = new StringBuilder();

            int rows = constraints.GetLength(0);
            int cols = constraints.GetLength(1);

            double[,] table = new double[rows + 1, cols + rows + 1];
            for (int i = 0; i < rows - 1; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    table[i, j] = constraints[i, j];
                }
                table[i, cols + i] = 1;
                table[i, cols + rows] = b[i];
            }

            for (int j = 0; j < cols - 1; j++)
            {
                table[rows, j] = -c[j];
            }

            int iteration = 0;
            while (true)
            {
                log.AppendLine($"Iteration {iteration++}");
                log.AppendLine(TableToString(table));

                int pivotCol = -1;
                double minValue = 0;
                for (int j = 0; j < cols + rows; j++)
                {
                    if (table[rows, j] < minValue)
                    {
                        minValue = table[rows, j];
                        pivotCol = j;
                    }
                }

                if (pivotCol == -1)
                    break;

                int pivotRow = -1;
                double minRatio = double.MaxValue;
                for (int i = 0; i < rows; i++)
                {
                    if (table[i, pivotCol] > 0)
                    {
                        double ratio = table[i, cols + rows] / table[i, pivotCol];
                        if (ratio < minRatio)
                        {
                            minRatio = ratio;
                            pivotRow = i;
                        }
                    }
                }

                if (pivotRow == -1)
                    throw new Exception("Задача не имеет конечного решения.");

                double pivotValue = table[pivotRow, pivotCol];
                for (int j = 0; j < cols + rows + 1; j++)
                {
                    table[pivotRow, j] /= pivotValue;
                }

                for (int i = 0; i < rows + 1; i++)
                {
                    if (i != pivotRow)
                    {
                        double factor = table[i, pivotCol];
                        for (int j = 0; j < cols + rows + 1; j++)
                        {
                            table[i, j] -= factor * table[pivotRow, j];
                        }
                    }
                }
            }

            log.AppendLine("Final Table:");
            log.AppendLine(TableToString(table));

            double[] solution = new double[cols];
            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    if (table[j, i] == 1)
                    {
                        solution[i] = table[j, cols + rows];
                        break;
                    }
                }
            }

            double maxProfit = table[rows, cols + rows];
            log.AppendLine($"Answer: {string.Join(", ", solution.Select(x => x.ToString("F2")))}");
            log.AppendLine($"Max Profit: {maxProfit:F2}");

            System.IO.File.WriteAllText(logPath, log.ToString());
            return Tuple.Create(solution, maxProfit);
        }

        private string TableToString(double[,] table)
        {
            var sb = new StringBuilder();
            int rows = table.GetLength(0);
            int cols = table.GetLength(1);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    sb.Append(table[i, j].ToString("F2")).Append('\t');
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
