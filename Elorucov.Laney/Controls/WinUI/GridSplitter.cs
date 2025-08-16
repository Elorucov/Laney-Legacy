using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Elorucov.Laney.Controls.WinUI {
    /// <summary>
    /// Enum to indicate whether GridSplitter resizes Columns or Rows
    /// </summary>
    public enum GridResizeDirection {
        /// <summary>
        /// Determines whether to resize rows or columns based on its Alignment and
        /// width compared to height
        /// </summary>
        Auto,

        /// <summary>
        /// Resize columns when dragging Splitter.
        /// </summary>
        Columns,

        /// <summary>
        /// Resize rows when dragging Splitter.
        /// </summary>
        Rows
    }

    /// <summary>
    /// Enum to indicate what Columns or Rows the GridSplitter resizes
    /// </summary>
    public enum GridResizeBehavior {
        /// <summary>
        /// Determine which columns or rows to resize based on its Alignment.
        /// </summary>
        BasedOnAlignment,

        /// <summary>
        /// Resize the current and next Columns or Rows.
        /// </summary>
        CurrentAndNext,

        /// <summary>
        /// Resize the previous and current Columns or Rows.
        /// </summary>
        PreviousAndCurrent,

        /// <summary>
        /// Resize the previous and next Columns or Rows.
        /// </summary>
        PreviousAndNext
    }

    /// <summary>
    /// Represents the control that redistributes space between columns or rows of a Grid control.
    /// </summary>
    public partial class GridSplitter : SizerBase {
        private GridResizeDirection _resizeDirection;
        private GridResizeBehavior _resizeBehavior;

        /// <summary>
        /// Identifies the <see cref="ResizeDirection"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResizeDirectionProperty
            = DependencyProperty.Register(
                nameof(ResizeDirection),
                typeof(GridResizeDirection),
                typeof(GridSplitter),
                new PropertyMetadata(GridResizeDirection.Auto, OnResizeDirectionPropertyChanged));

        private static void OnResizeDirectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is GridSplitter splitter && e.NewValue is GridResizeDirection direction &&
                direction != GridResizeDirection.Auto) {
                // Update base classes property based on specific polyfill for GridSplitter
                splitter.Orientation =
                    direction == GridResizeDirection.Rows ?
                        Orientation.Horizontal :
                        Orientation.Vertical;
            }
        }

        /// <summary>
        /// Identifies the <see cref="ResizeBehavior"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResizeBehaviorProperty
            = DependencyProperty.Register(
                nameof(ResizeBehavior),
                typeof(GridResizeBehavior),
                typeof(GridSplitter),
                new PropertyMetadata(GridResizeBehavior.BasedOnAlignment));

        /// <summary>
        /// Identifies the <see cref="ParentLevel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ParentLevelProperty
            = DependencyProperty.Register(
                nameof(ParentLevel),
                typeof(int),
                typeof(GridSplitter),
                new PropertyMetadata(default(int)));

        /// <summary>
        /// Gets or sets whether the Splitter resizes the Columns, Rows, or Both.
        /// </summary>
        public GridResizeDirection ResizeDirection {
            get { return (GridResizeDirection)GetValue(ResizeDirectionProperty); }
            set { SetValue(ResizeDirectionProperty, value); }
        }

        /// <summary>
        /// Gets or sets which Columns or Rows the Splitter resizes.
        /// </summary>
        public GridResizeBehavior ResizeBehavior {
            get { return (GridResizeBehavior)GetValue(ResizeBehaviorProperty); }
            set { SetValue(ResizeBehaviorProperty, value); }
        }

        /// <summary>
        /// Gets or sets the level of the parent grid to resize
        /// </summary>
        public int ParentLevel {
            get { return (int)GetValue(ParentLevelProperty); }
            set { SetValue(ParentLevelProperty, value); }
        }

        /// <summary>
        /// Gets the target parent grid from level
        /// </summary>
        private FrameworkElement TargetControl {
            get {
                if (ParentLevel == 0) {
                    return this;
                }

                // TODO: Can we just use our Visual/Logical Tree extensions for this?
                var parent = Parent;
                for (int i = 2; i < ParentLevel; i++) // TODO: Why is this 2? We need better documentation on ParentLevel
                {
                    if (parent is FrameworkElement frameworkElement) {
                        parent = frameworkElement.Parent;
                    } else {
                        break;
                    }
                }

                return parent as FrameworkElement;
            }
        }

        /// <summary>
        /// Gets GridSplitter Container Grid
        /// </summary>
        private Grid Resizable => TargetControl?.Parent as Grid;

        /// <summary>
        /// Gets the current Column definition of the parent Grid
        /// </summary>
        private ColumnDefinition CurrentColumn {
            get {
                if (Resizable == null) {
                    return null;
                }

                var gridSplitterTargetedColumnIndex = GetTargetedColumn();

                if ((gridSplitterTargetedColumnIndex >= 0)
                    && (gridSplitterTargetedColumnIndex < Resizable.ColumnDefinitions.Count)) {
                    return Resizable.ColumnDefinitions[gridSplitterTargetedColumnIndex];
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the Sibling Column definition of the parent Grid
        /// </summary>
        private ColumnDefinition SiblingColumn {
            get {
                if (Resizable == null) {
                    return null;
                }

                var gridSplitterSiblingColumnIndex = GetSiblingColumn();

                if ((gridSplitterSiblingColumnIndex >= 0)
                    && (gridSplitterSiblingColumnIndex < Resizable.ColumnDefinitions.Count)) {
                    return Resizable.ColumnDefinitions[gridSplitterSiblingColumnIndex];
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the current Row definition of the parent Grid
        /// </summary>
        private RowDefinition CurrentRow {
            get {
                if (Resizable == null) {
                    return null;
                }

                var gridSplitterTargetedRowIndex = GetTargetedRow();

                if ((gridSplitterTargetedRowIndex >= 0)
                    && (gridSplitterTargetedRowIndex < Resizable.RowDefinitions.Count)) {
                    return Resizable.RowDefinitions[gridSplitterTargetedRowIndex];
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the Sibling Row definition of the parent Grid
        /// </summary>
        private RowDefinition SiblingRow {
            get {
                if (Resizable == null) {
                    return null;
                }

                var gridSplitterSiblingRowIndex = GetSiblingRow();

                if ((gridSplitterSiblingRowIndex >= 0)
                    && (gridSplitterSiblingRowIndex < Resizable.RowDefinitions.Count)) {
                    return Resizable.RowDefinitions[gridSplitterSiblingRowIndex];
                }

                return null;
            }
        }

        protected override void OnLoaded(RoutedEventArgs e) {
            _resizeDirection = GetResizeDirection();
            Orientation = _resizeDirection == GridResizeDirection.Rows ?
                Orientation.Horizontal : Orientation.Vertical;
            _resizeBehavior = GetResizeBehavior();
        }

        private double _currentSize;
        private double _siblingSize;

        /// <inheritdoc />
        protected override void OnDragStarting() {
            _resizeDirection = GetResizeDirection();
            Orientation = _resizeDirection == GridResizeDirection.Rows ?
                Orientation.Horizontal : Orientation.Vertical;
            _resizeBehavior = GetResizeBehavior();

            // Record starting points
            if (Orientation == Orientation.Horizontal) {
                _currentSize = CurrentRow?.ActualHeight ?? -1;
                _siblingSize = SiblingRow?.ActualHeight ?? -1;
            } else {
                _currentSize = CurrentColumn?.ActualWidth ?? -1;
                _siblingSize = SiblingColumn?.ActualWidth ?? -1;
            }
        }

        /// <inheritdoc/>
        protected override bool OnDragVertical(double verticalChange) {
            if (CurrentRow == null || SiblingRow == null || Resizable == null) {
                return false;
            }

            var currentChange = _currentSize + verticalChange;
            var siblingChange = _siblingSize + (verticalChange * -1); // sibling moves opposite

            // Would changing the columnn sizes violate the constraints?
            if (!IsValidRowHeight(CurrentRow, currentChange) || !IsValidRowHeight(SiblingRow, siblingChange)) {
                return false;
            }

            // NOTE: If the column contains another row with Star sizing, it's not enough to just change current.
            // The change will flow to the Star sized item and not to the sibling if the sibling is fixed-size.
            // So, we need to explicitly apply the change to the sibling.

            // if current row has fixed height then resize it
            if (!IsStarRow(CurrentRow)) {
                // No need to check for the row Min height because it is automatically respected
                var changed = SetRowHeight(CurrentRow, currentChange, GridUnitType.Pixel);

                if (!IsStarRow(SiblingRow)) {
                    changed = SetRowHeight(SiblingRow, siblingChange, GridUnitType.Pixel);
                }

                return changed;
            }

            // if sibling row has fixed width then resize it
            else if (!IsStarRow(SiblingRow)) {
                return SetRowHeight(SiblingRow, siblingChange, GridUnitType.Pixel);
            }

            // if both row haven't fixed height (auto *)
            else {
                // change current row height to the new height with respecting the auto
                // change sibling row height to the new height relative to current row
                // respect the other star row height by setting it's height to it's actual height with stars

                // We need to validate current and sibling height to not cause any unexpected behavior
                if (!IsValidRowHeight(CurrentRow, currentChange) ||
                    !IsValidRowHeight(SiblingRow, siblingChange)) {
                    return false;
                }

                foreach (var rowDefinition in Resizable.RowDefinitions) {
                    if (rowDefinition == CurrentRow) {
                        SetRowHeight(CurrentRow, currentChange, GridUnitType.Star);
                    } else if (rowDefinition == SiblingRow) {
                        SetRowHeight(SiblingRow, siblingChange, GridUnitType.Star);
                    } else if (IsStarRow(rowDefinition)) {
                        rowDefinition.Height = new GridLength(rowDefinition.ActualHeight, GridUnitType.Star);
                    }
                }

                return true;
            }
        }

        /// <inheritdoc/>
        protected override bool OnDragHorizontal(double horizontalChange) {
            if (CurrentColumn == null || SiblingColumn == null || Resizable == null) {
                return false;
            }

            var currentChange = _currentSize + horizontalChange;
            var siblingChange = _siblingSize + (horizontalChange * -1); // sibling moves opposite

            // Would changing the columnn sizes violate the constraints?
            if (!IsValidColumnWidth(CurrentColumn, currentChange) || !IsValidColumnWidth(SiblingColumn, siblingChange)) {
                return false;
            }

            // NOTE: If the row contains another column with Star sizing, it's not enough to just change current.
            // The change will flow to the Star sized item and not to the sibling if the sibling is fixed-size.
            // So, we need to explicitly apply the change to the sibling.

            // if current column has fixed width then resize it
            if (!IsStarColumn(CurrentColumn)) {
                // No need to check for the Column Min width because it is automatically respected
                var changed = SetColumnWidth(CurrentColumn, currentChange, GridUnitType.Pixel);

                if (!IsStarColumn(SiblingColumn)) {
                    changed = SetColumnWidth(SiblingColumn, siblingChange, GridUnitType.Pixel);
                }

                return changed;
            }

            // if sibling column has fixed width then resize it
            else if (!IsStarColumn(SiblingColumn)) {
                return SetColumnWidth(SiblingColumn, siblingChange, GridUnitType.Pixel);
            }

            // if both column haven't fixed width (auto *)
            else {
                // change current column width to the new width with respecting the auto
                // change sibling column width to the new width relative to current column
                // respect the other star column width by setting it's width to it's actual width with stars

                // We need to validate current and sibling width to not cause any unexpected behavior
                if (!IsValidColumnWidth(CurrentColumn, currentChange) ||
                    !IsValidColumnWidth(SiblingColumn, siblingChange)) {
                    return false;
                }

                foreach (var columnDefinition in Resizable.ColumnDefinitions) {
                    if (columnDefinition == CurrentColumn) {
                        SetColumnWidth(CurrentColumn, currentChange, GridUnitType.Star);
                    } else if (columnDefinition == SiblingColumn) {
                        SetColumnWidth(SiblingColumn, siblingChange, GridUnitType.Star);
                    } else if (IsStarColumn(columnDefinition)) {
                        columnDefinition.Width = new GridLength(columnDefinition.ActualWidth, GridUnitType.Star);
                    }
                }

                return true;
            }
        }

        private static bool IsStarColumn(ColumnDefinition definition) {
            return ((GridLength)definition.GetValue(ColumnDefinition.WidthProperty)).IsStar;
        }

        private static bool IsStarRow(RowDefinition definition) {
            return ((GridLength)definition.GetValue(RowDefinition.HeightProperty)).IsStar;
        }

        private bool SetColumnWidth(ColumnDefinition columnDefinition, double newWidth, GridUnitType unitType) {
            var minWidth = columnDefinition.MinWidth;
            if (!double.IsNaN(minWidth) && newWidth < minWidth) {
                newWidth = minWidth;
            }

            var maxWidth = columnDefinition.MaxWidth;
            if (!double.IsNaN(maxWidth) && newWidth > maxWidth) {
                newWidth = maxWidth;
            }

            if (newWidth > ActualWidth) {
                columnDefinition.Width = new GridLength(newWidth, unitType);
                return true;
            }

            return false;
        }

        private bool IsValidColumnWidth(ColumnDefinition columnDefinition, double newWidth) {
            var minWidth = columnDefinition.MinWidth;
            if (!double.IsNaN(minWidth) && newWidth < minWidth) {
                return false;
            }

            var maxWidth = columnDefinition.MaxWidth;
            if (!double.IsNaN(maxWidth) && newWidth > maxWidth) {
                return false;
            }

            if (newWidth <= ActualWidth) {
                return false;
            }

            return true;
        }

        private bool SetRowHeight(RowDefinition rowDefinition, double newHeight, GridUnitType unitType) {
            var minHeight = rowDefinition.MinHeight;
            if (!double.IsNaN(minHeight) && newHeight < minHeight) {
                newHeight = minHeight;
            }

            var maxWidth = rowDefinition.MaxHeight;
            if (!double.IsNaN(maxWidth) && newHeight > maxWidth) {
                newHeight = maxWidth;
            }

            if (newHeight > ActualHeight) {
                rowDefinition.Height = new GridLength(newHeight, unitType);
                return true;
            }

            return false;
        }

        private bool IsValidRowHeight(RowDefinition rowDefinition, double newHeight) {
            var minHeight = rowDefinition.MinHeight;
            if (!double.IsNaN(minHeight) && newHeight < minHeight) {
                return false;
            }

            var maxHeight = rowDefinition.MaxHeight;
            if (!double.IsNaN(maxHeight) && newHeight > maxHeight) {
                return false;
            }

            if (newHeight <= ActualHeight) {
                return false;
            }

            return true;
        }

        // Return the targeted Column based on the resize behavior
        private int GetTargetedColumn() {
            var currentIndex = Grid.GetColumn(TargetControl);
            return GetTargetIndex(currentIndex);
        }

        // Return the sibling Row based on the resize behavior
        private int GetTargetedRow() {
            var currentIndex = Grid.GetRow(TargetControl);
            return GetTargetIndex(currentIndex);
        }

        // Return the sibling Column based on the resize behavior
        private int GetSiblingColumn() {
            var currentIndex = Grid.GetColumn(TargetControl);
            return GetSiblingIndex(currentIndex);
        }

        // Return the sibling Row based on the resize behavior
        private int GetSiblingRow() {
            var currentIndex = Grid.GetRow(TargetControl);
            return GetSiblingIndex(currentIndex);
        }

        // Gets index based on resize behavior for first targeted row/column
        private int GetTargetIndex(int currentIndex) {
            switch (_resizeBehavior) {
                case GridResizeBehavior.CurrentAndNext:
                    return currentIndex;
                case GridResizeBehavior.PreviousAndNext:
                    return currentIndex - 1;
                case GridResizeBehavior.PreviousAndCurrent:
                    return currentIndex - 1;
                default:
                    return -1;
            }
        }

        // Gets index based on resize behavior for second targeted row/column
        private int GetSiblingIndex(int currentIndex) {
            switch (_resizeBehavior) {
                case GridResizeBehavior.CurrentAndNext:
                    return currentIndex + 1;
                case GridResizeBehavior.PreviousAndNext:
                    return currentIndex + 1;
                case GridResizeBehavior.PreviousAndCurrent:
                    return currentIndex;
                default:
                    return -1;
            }
        }

        // Checks the control alignment and Width/Height to detect the control resize direction columns/rows
        private GridResizeDirection GetResizeDirection() {
            GridResizeDirection direction = ResizeDirection;

            if (direction == GridResizeDirection.Auto) {
                // When HorizontalAlignment is Left, Right or Center, resize Columns
                if (HorizontalAlignment != HorizontalAlignment.Stretch) {
                    direction = GridResizeDirection.Columns;
                }

                // When VerticalAlignment is Top, Bottom or Center, resize Rows
                else if (VerticalAlignment != VerticalAlignment.Stretch) {
                    direction = GridResizeDirection.Rows;
                }

                // Check Width vs Height
                else if (ActualWidth <= ActualHeight) {
                    direction = GridResizeDirection.Columns;
                } else {
                    direction = GridResizeDirection.Rows;
                }
            }

            return direction;
        }

        // Get the resize behavior (Which columns/rows should be resized) based on alignment and Direction
        private GridResizeBehavior GetResizeBehavior() {
            GridResizeBehavior resizeBehavior = ResizeBehavior;

            if (resizeBehavior == GridResizeBehavior.BasedOnAlignment) {
                if (_resizeDirection == GridResizeDirection.Columns) {
                    switch (HorizontalAlignment) {
                        case HorizontalAlignment.Left:
                            resizeBehavior = GridResizeBehavior.PreviousAndCurrent;
                            break;
                        case HorizontalAlignment.Right:
                            resizeBehavior = GridResizeBehavior.CurrentAndNext;
                            break;
                        default:
                            resizeBehavior = GridResizeBehavior.PreviousAndNext;
                            break;
                    }
                }

                // resize direction is vertical
                else {
                    switch (VerticalAlignment) {
                        case VerticalAlignment.Top:
                            resizeBehavior = GridResizeBehavior.PreviousAndCurrent;
                            break;
                        case VerticalAlignment.Bottom:
                            resizeBehavior = GridResizeBehavior.CurrentAndNext;
                            break;
                        default:
                            resizeBehavior = GridResizeBehavior.PreviousAndNext;
                            break;
                    }
                }
            }

            return resizeBehavior;
        }
    }
}
