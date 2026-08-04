namespace LoogaSoft.UI.Extensions
{
    /// <summary>Defines how a layout arranges its direct children.</summary>
    public enum LoogaLayoutMode
    {
        Horizontal,
        Vertical,
        Grid,
        Flow,
        Overlay
    }

    /// <summary>Defines how a layout calculates its own size.</summary>
    public enum LoogaLayoutSizeMode
    {
        Authored,
        Content,
        FillParent,
        Fixed,
        ClampedContent
    }

    /// <summary>Defines how a layout calculates each child size.</summary>
    public enum LoogaLayoutChildSizeMode
    {
        Content,
        Fill,
        Uniform,
        Fixed,
        Authored
    }

    /// <summary>Defines which grid dimension has a fixed item count.</summary>
    public enum LoogaGridConstraint
    {
        Flexible,
        FixedColumns,
        FixedRows
    }

    /// <summary>Defines how a grid calculates its cell size.</summary>
    public enum LoogaGridCellMode
    {
        Fixed,
        LargestContent
    }

    /// <summary>Defines the RectTransform that supplies content measurements.</summary>
    public enum LoogaContentSource
    {
        Self,
        FirstChild,
        Assigned
    }

    /// <summary>Defines how a fitter converts content measurements into size.</summary>
    public enum LoogaContentFitMode
    {
        Authored,
        Minimum,
        Preferred,
        ClampedPreferred
    }
}
