using Jarvis.Desktop.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace Jarvis.Desktop.Converters;

public class SegmentTemplateSelector : DataTemplateSelector
{
    public DataTemplate? CodeTemplate { get; set; }
    public DataTemplate? TextTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is MarkdownSegment seg)
            return seg.IsCode ? CodeTemplate : TextTemplate;
        return base.SelectTemplate(item, container);
    }
}
