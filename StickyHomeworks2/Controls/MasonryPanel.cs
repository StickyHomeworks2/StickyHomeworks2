using System.Windows;
using System.Windows.Controls;

namespace StickyHomeworks.Controls;

public class MasonryPanel : Panel
{
    public static readonly DependencyProperty ItemWidthProperty =
        DependencyProperty.Register(nameof(ItemWidth), typeof(double), typeof(MasonryPanel),
            new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double ItemWidth
    {
        get => (double)GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    private record struct ItemLayout(int Column, double Y);

    private readonly List<ItemLayout> _itemLayouts = [];
    private readonly Dictionary<string, int> _previousColumns = [];

    protected override Size MeasureOverride(Size availableSize)
    {
        var itemWidth = ResolveItemWidth(availableSize);
        if (itemWidth <= 0)
            return new Size(0, 0);

        _itemLayouts.Clear();
        var columnHeights = new List<double>();
        var hasHeightLimit = !double.IsInfinity(availableSize.Height);
        var currentColumns = new Dictionary<string, int>();

        foreach (UIElement child in InternalChildren)
        {
            child.Measure(new Size(itemWidth, double.PositiveInfinity));
            var childHeight = child.DesiredSize.Height;
            var key = (child as FrameworkElement)?.DataContext?.ToString();

            int column;
            if (hasHeightLimit)
            {
                if (key != null
                    && _previousColumns.TryGetValue(key, out var prevCol)
                    && prevCol < columnHeights.Count
                    && columnHeights[prevCol] + childHeight <= availableSize.Height)
                {
                    column = prevCol;
                }
                else
                {
                    column = FindFittingColumn(columnHeights, childHeight, availableSize.Height);
                }
            }
            else
            {
                column = FindMinColumn(columnHeights, itemWidth, availableSize.Width);
            }

            _itemLayouts.Add(new ItemLayout(column, columnHeights[column]));
            columnHeights[column] += childHeight;

            if (key != null)
                currentColumns[key] = column;
        }

        _previousColumns.Clear();
        foreach (var kv in currentColumns)
            _previousColumns[kv.Key] = kv.Value;

        return columnHeights.Count == 0
            ? new Size(0, 0)
            : new Size(columnHeights.Count * itemWidth, columnHeights.Max());
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var itemWidth = ResolveItemWidth(finalSize);
        if (itemWidth <= 0)
            return finalSize;

        for (var i = 0; i < InternalChildren.Count && i < _itemLayouts.Count; i++)
        {
            var layout = _itemLayouts[i];
            InternalChildren[i].Arrange(new Rect(
                layout.Column * itemWidth, layout.Y,
                itemWidth, InternalChildren[i].DesiredSize.Height));
        }

        return finalSize;
    }

    private double ResolveItemWidth(Size availableSize)
    {
        var w = ItemWidth;
        return double.IsNaN(w) || w <= 0
            ? (double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width)
            : w;
    }

    private static int FindFittingColumn(List<double> columnHeights, double childHeight, double maxHeight)
    {
        var best = -1;
        var bestHeight = double.MaxValue;

        for (var i = 0; i < columnHeights.Count; i++)
        {
            if (columnHeights[i] + childHeight <= maxHeight && columnHeights[i] < bestHeight)
            {
                best = i;
                bestHeight = columnHeights[i];
            }
        }

        if (best == -1)
        {
            best = columnHeights.Count;
            columnHeights.Add(0);
        }

        return best;
    }

    private static int FindMinColumn(List<double> columnHeights, double itemWidth, double availableWidth)
    {
        if (columnHeights.Count == 0)
        {
            var columns = double.IsInfinity(availableWidth)
                ? 1
                : Math.Max(1, (int)(availableWidth / itemWidth));
            for (var i = 0; i < columns; i++)
                columnHeights.Add(0);
        }

        var minIndex = 0;
        for (var i = 1; i < columnHeights.Count; i++)
        {
            if (columnHeights[i] < columnHeights[minIndex])
                minIndex = i;
        }
        return minIndex;
    }
}
