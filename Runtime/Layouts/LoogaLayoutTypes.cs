namespace LoogaSoft.UI.Extensions
{
    public enum LoogaLayoutMode
    {
        Horizontal,
        Vertical,
        Grid,
        Flow,
        Overlay
    }

    public enum LoogaLayoutSizeMode
    {
        Authored,
        Content,
        FillParent,
        Fixed,
        ClampedContent
    }

    public enum LoogaLayoutChildSizeMode
    {
        Content,
        Fill,
        Uniform,
        Fixed,
        Authored
    }

    public enum LoogaGridConstraint
    {
        Flexible,
        FixedColumns,
        FixedRows
    }

    public enum LoogaGridCellMode
    {
        Fixed,
        LargestContent
    }

    public enum LoogaContentSource
    {
        Self,
        FirstChild,
        Assigned
    }

    public enum LoogaContentFitMode
    {
        Authored,
        Minimum,
        Preferred,
        ClampedPreferred
    }
}
