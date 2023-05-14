﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace charposition.Controls;

public class CharsView : UserControl
{
    private const int CellWidth = 24;
    private const int CellHeight = 32;

    private readonly Brush LineBrush = Brushes.Gray;
    private readonly Brush LabelBrush = Brushes.Gray;

    public int LineCount
    {
        get => (int)GetValue(LineCountProperty);
        set => SetValue(LineCountProperty, value);
    }
    public static readonly DependencyProperty LineCountProperty =
        DependencyProperty.Register("LineCount", typeof(int), typeof(CharsView), new PropertyMetadata(1, Redraw));

    public int MaxLineLength
    {
        get => (int)GetValue(MaxLineLengthProperty);
        set => SetValue(MaxLineLengthProperty, value);
    }
    public static readonly DependencyProperty MaxLineLengthProperty =
        DependencyProperty.Register("MaxLineLength", typeof(int), typeof(CharsView), new PropertyMetadata(1, Redraw));

    public IEnumerable LineChars
    {
        get => (IEnumerable)GetValue(LineCharsProperty);
        set => SetValue(LineCharsProperty, value);
    }
    public static readonly DependencyProperty LineCharsProperty =
            DependencyProperty.Register("LineChars", typeof(IEnumerable), typeof(CharsView), new PropertyMetadata(null, OnLineCharsChanged));

    static void OnLineCharsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ObservableCollection<char[]> coll && d is CharsView ctrl)
        {
            coll.CollectionChanged += (object? sender, NotifyCollectionChangedEventArgs e) => ctrl.Draw();
        }
    }

    public CharsView()
    {
        this.Canvas = new Canvas();
        this.Content = this.Canvas;

        Canvas.SetBinding(WidthProperty,
                new Binding { Path = new PropertyPath(ActualWidthProperty), Source = this });
        Canvas.SetBinding(HeightProperty,
            new Binding { Path = new PropertyPath(ActualHeightProperty), Source = this });

        this.SizeChanged += (s, e) => this.Draw();

        this.HorizontalLine = new Line
        {
            Stroke = LineBrush,
            StrokeThickness = 1,
            X1 = CellWidth,
            Y1 = CellWidth,
            Y2 = CellWidth
        };
        this.Canvas.Children.Add(this.HorizontalLine);

        this.VerticalLine = new Line
        {
            Stroke = LineBrush,
            StrokeThickness = 1,
            X1 = CellWidth,
            X2 = CellWidth,
            Y1 = CellWidth
        };
        this.Canvas.Children.Add(this.VerticalLine);
    }

    private static void Redraw(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((CharsView)d).Draw();

    private void Draw()
    {
        this.UpdateBorder();
        this.UpdateLineNumbers();
        this.UpdateColumnNumbers();
        this.UpdateRowLines();
        this.UpdateColumnLines();
        this.UpdateCharacters();
    }

    private void UpdateCharacters()
    {
        int totalChars = 0;
        int lineNo = 0;
        foreach (var line in this.LineChars)
        {
            if (line is not char[] chars)
            {
                continue;
            }

            for (int i = 0; i < chars.Length; i++)
            {
                var character = new Character(totalChars++, chars[i])
                {
                    Width = CellWidth,
                    Height = CellHeight
                };
                this.Canvas.Children.Add(character);
                Canvas.SetTop(character, CellWidth + (lineNo * CellHeight));
                Canvas.SetLeft(character, (i + 1) * CellWidth);
            }
            lineNo++;
        }
    }

    private void UpdateColumnLines()
    {
        int row = 0;
        int index = 0;
        foreach (char[] lineChars in this.LineChars)
        {
            for (int i = 1; i <= lineChars.Length; i++)
            {
                if (this.ColumnLines.Count <= index)
                {
                    var line = new Line
                    {
                        Stroke = LineBrush,
                        StrokeThickness = 0.5,
                    };
                    this.Canvas.Children.Add(line);
                    this.ColumnLines.Add(line);
                }
                var columnLine = this.ColumnLines[index++];

                columnLine.X1 = columnLine.X2 = CellWidth + (i * CellWidth);
                columnLine.Y1 = CellWidth + (row * CellHeight);
                columnLine.Y2 = CellWidth + ((row + 1) * CellHeight);
            }

            row++;
        }

        while (this.ColumnLines.Count > index)
        {
            this.Canvas.Children.Remove(this.ColumnLines[^1]);
            this.ColumnLines.RemoveAt(this.ColumnLines.Count - 1);
        }
    }

    private void UpdateRowLines()
    {
        while (this.RowLines.Count > this.LineCount)
        {
            this.Canvas.Children.Remove(this.RowLines[^1]);
            this.RowLines.RemoveAt(this.RowLines.Count - 1);
        }
        while (this.RowLines.Count < this.LineCount)
        {
            var line = new Line
            {
                Stroke = LineBrush,
                StrokeThickness = 0.5,
                X1 = CellWidth
            };
            this.Canvas.Children.Add(line);
            this.RowLines.Add(line);
        }
        int row = 0;
        int lastLineLength = 0;
        foreach (char[] lineChars in this.LineChars)
        {
            if (row == 0)
            {
                row++;
                lastLineLength = lineChars.Length;
                continue;
            }
            int length = Math.Max(lastLineLength, lineChars.Length);
            UpdateRowLineLength(row, length);
            lastLineLength = lineChars.Length;
            row++;
        }
        UpdateRowLineLength(row, lastLineLength);
    }

    private void UpdateRowLineLength(int row, int length)
    {
        if (row < 1)
        {
            return;
        }

        var line = this.RowLines[row - 1];
        line.Y1 = CellWidth + (row * CellHeight);
        line.Y2 = CellWidth + (row * CellHeight);
        line.X2 = CellWidth + (length * CellWidth);
    }

    private void UpdateColumnNumbers()
    {
        while (this.ColumnLabels.Count > this.MaxLineLength)
        {
            this.Canvas.Children.Remove(this.ColumnLabels[^1]);
            this.ColumnLabels.RemoveAt(this.ColumnLabels.Count - 1);
        }
        while (this.ColumnLabels.Count < this.MaxLineLength)
        {
            var label = new TextBlock
            {
                Text = (this.ColumnLabels.Count + 1).ToString(),
                Foreground = LabelBrush,
                FontSize = 12,
                Width = CellWidth,
                Height = CellHeight,
                TextAlignment = TextAlignment.Center
            };
            this.Canvas.Children.Add(label);
            this.ColumnLabels.Add(label);
            Canvas.SetTop(label, 0);
            Canvas.SetLeft(label, this.ColumnLabels.Count * CellWidth);
        }
    }

    private void UpdateLineNumbers()
    {
        while (this.LineNoLabels.Count > this.LineCount)
        {
            this.Canvas.Children.Remove(this.LineNoLabels[^1]);
            this.LineNoLabels.RemoveAt(this.LineNoLabels.Count - 1);
        }
        while (this.LineNoLabels.Count < this.LineCount)
        {
            var label = new TextBlock
            {
                Text = (this.LineNoLabels.Count + 1).ToString(),
                Foreground = LabelBrush,
                FontSize = 12,
                Width = CellWidth,
                Height = CellHeight,
                TextAlignment = TextAlignment.Center
            };
            this.Canvas.Children.Add(label);
            this.LineNoLabels.Add(label);
            Canvas.SetTop(label, this.LineNoLabels.Count * CellHeight);
            Canvas.SetLeft(label, 0);
        }
    }

    private void UpdateBorder()
    {
        this.HorizontalLine.X2 = this.ActualWidth;
        this.VerticalLine.Y2 = this.ActualHeight;
    }

    private Canvas Canvas { get; }
    private Line HorizontalLine { get; }
    private Line VerticalLine { get; }

    private List<TextBlock> LineNoLabels { get; } = new();
    private List<TextBlock> ColumnLabels { get; } = new();

    private List<Line> RowLines { get; } = new();
    private List<Line> ColumnLines { get; } = new();

    private List<Character> Characters { get; } = new();
}
