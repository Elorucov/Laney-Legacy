using System;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using CursorEnum = Windows.UI.Core.CoreCursorType;

namespace Elorucov.Laney.Controls.WinUI {
    public class SizerAutomationPeer : FrameworkElementAutomationPeer {
        /// <summary>
        /// Initializes a new instance of the <see cref="SizerAutomationPeer"/> class.
        /// </summary>
        /// <param name="owner">
        /// The <see cref="SizerBase" /> that is associated with this <see cref="SizerAutomationPeer" />.
        /// </param>
        public SizerAutomationPeer(SizerBase owner)
            : base(owner) {
        }

        private SizerBase OwningSizer {
            get {
                return Owner as SizerBase;
            }
        }

        /// <summary>
        /// Called by GetClassName that gets a human readable name that, in addition to AutomationControlType,
        /// differentiates the control represented by this AutomationPeer.
        /// </summary>
        /// <returns>The string that contains the name.</returns>
        protected override string GetClassNameCore() {
            return Owner.GetType().Name;
        }

        /// <summary>
        /// Called by GetName.
        /// </summary>
        /// <returns>
        /// Returns the first of these that is not null or empty:
        /// - Value returned by the base implementation
        /// - Name of the owning ContentSizer
        /// - ContentSizer class name
        /// </returns>
        protected override string GetNameCore() {
            string name = AutomationProperties.GetName(this.OwningSizer);
            if (!string.IsNullOrEmpty(name)) {
                return name;
            }

            name = this.OwningSizer.Name;
            if (!string.IsNullOrEmpty(name)) {
                return name;
            }

            name = base.GetNameCore();
            if (!string.IsNullOrEmpty(name)) {
                return name;
            }

            return string.Empty;
        }
    }

    [TemplateVisualState(Name = NormalState, GroupName = CommonStates)]
    [TemplateVisualState(Name = PointerOverState, GroupName = CommonStates)]
    [TemplateVisualState(Name = PressedState, GroupName = CommonStates)]
    [TemplateVisualState(Name = DisabledState, GroupName = CommonStates)]
    [TemplateVisualState(Name = HorizontalState, GroupName = OrientationStates)]
    [TemplateVisualState(Name = VerticalState, GroupName = OrientationStates)]
    [TemplateVisualState(Name = VisibleState, GroupName = ThumbVisibilityStates)]
    [TemplateVisualState(Name = CollapsedState, GroupName = ThumbVisibilityStates)]
    public abstract partial class SizerBase : Control {
        /// <summary>
        /// Gets or sets the cursor to use when hovering over the gripper bar. If left as <c>null</c>, the control will manage the cursor automatically based on the <see cref="Orientation"/> property value (default).
        /// </summary>
        public CursorEnum Cursor {
            get { return (CursorEnum)GetValue(CursorProperty); }
            set { SetValue(CursorProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Cursor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CursorProperty =
            DependencyProperty.Register(nameof(Cursor), typeof(CursorEnum), typeof(SizerBase), new PropertyMetadata(null, OnOrientationPropertyChanged));

        /// <summary>
        /// Gets or sets the incremental amount of change for dragging with the mouse or touch of a sizer control. Effectively a snapping increment for changes. The default is 1.
        /// </summary>
        /// <example>
        /// For instance, if the DragIncrement is set to 16. Then when a component is resized with the sizer, it will only increase or decrease in size in that increment. I.e. -16, 0, 16, 32, 48, etc...
        /// </example>
        /// <remarks>
        /// This value is independent of the <see cref="KeyboardIncrement"/> property. If you need to provide consistent snapping when moving regardless of input device, set these properties to the same value.
        /// </remarks>
        public double DragIncrement {
            get { return (double)GetValue(DragIncrementProperty); }
            set { SetValue(DragIncrementProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="DragIncrement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DragIncrementProperty =
            DependencyProperty.Register(nameof(DragIncrement), typeof(double), typeof(SizerBase), new PropertyMetadata(1d));

        /// <summary>
        /// Gets or sets the distance each press of an arrow key moves a sizer control. The default is 8.
        /// </summary>
        /// <remarks>
        /// This value is independent of the <see cref="DragIncrement"/> setting when using mouse/touch. If you want a consistent behavior regardless of input device, set them to the same value if snapping is required.
        /// </remarks>
        public double KeyboardIncrement {
            get { return (double)GetValue(KeyboardIncrementProperty); }
            set { SetValue(KeyboardIncrementProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="KeyboardIncrement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty KeyboardIncrementProperty =
            DependencyProperty.Register(nameof(KeyboardIncrement), typeof(double), typeof(SizerBase), new PropertyMetadata(8d));

        /// <summary>
        /// Gets or sets the orientation the sizer will be and how it will interact with other elements. Defaults to <see cref="Orientation.Vertical"/>.
        /// </summary>
        /// <remarks>
        /// Note if using <see cref="GridSplitter"/>, use the <see cref="GridSplitter.ResizeDirection"/> property instead.
        /// </remarks>
        public Orientation Orientation {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Orientation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(SizerBase), new PropertyMetadata(Orientation.Vertical, OnOrientationPropertyChanged));

        /// <summary>
        /// Gets or sets if the Thumb is visible. If not visible, only the background and cursor will be shown on MouseOver or Pressed states.
        /// </summary>
        public bool IsThumbVisible {
            get { return (bool)GetValue(IsThumbVisibleProperty); }
            set { SetValue(IsThumbVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsThumbVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsThumbVisibleProperty =
            DependencyProperty.Register(nameof(IsThumbVisible), typeof(bool), typeof(SizerBase), new PropertyMetadata(true, OnIsThumbVisiblePropertyChanged));


        private static void OnOrientationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is SizerBase gripper) {
                VisualStateManager.GoToState(gripper, gripper.Orientation == Orientation.Vertical ? VerticalState : HorizontalState, true);

                CursorEnum cursorByOrientation = gripper.Orientation == Orientation.Vertical ? CursorEnum.SizeWestEast : CursorEnum.SizeNorthSouth;

                // See if there's been a cursor override, otherwise we'll pick
                var cursor = gripper.ReadLocalValue(CursorProperty);
                if (cursor == DependencyProperty.UnsetValue || cursor == null) {
                    cursor = cursorByOrientation;
                }


                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8)) {
                    if (cursor is CursorEnum cursorValue) {
                        gripper.SetValue(CursorProperty, cursorValue);
                    }
                }

                return;
            }
        }
        private static void OnIsThumbVisiblePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is SizerBase gripper) {
                VisualStateManager.GoToState(gripper, gripper.IsThumbVisible ? VisibleState : CollapsedState, true);
            }
        }

        internal const string CommonStates = "CommonStates";
        internal const string NormalState = "Normal";
        internal const string PointerOverState = "PointerOver";
        internal const string PressedState = "Pressed";
        internal const string DisabledState = "Disabled";
        internal const string OrientationStates = "OrientationStates";
        internal const string HorizontalState = "Horizontal";
        internal const string VerticalState = "Vertical";
        internal const string ThumbVisibilityStates = "ThumbVisibilityStates";
        internal const string VisibleState = "Visible";
        internal const string CollapsedState = "Collapsed";
        /// <summary>
        /// Called when the control has been initialized.
        /// </summary>
        /// <param name="e">Loaded event args.</param>
        protected virtual void OnLoaded(RoutedEventArgs e) {
        }

        /// <summary>
        /// Called when the <see cref="SizerBase"/> control starts to be dragged by the user.
        /// Implementer should record current state of manipulated target at this point in time.
        /// They will receive the cumulative change in <see cref="OnDragHorizontal(double)"/> or
        /// <see cref="OnDragVertical(double)"/> based on the <see cref="Orientation"/> property.
        /// </summary>
        /// <remarks>
        /// This method is also called at the start of a keyboard interaction. Keyboard strokes use the same pattern to emulate a mouse movement for a single change. The appropriate
        /// <see cref="OnDragHorizontal(double)"/> or <see cref="OnDragVertical(double)"/>
        /// method will also be called after when the keyboard is used.
        /// </remarks>
        protected abstract void OnDragStarting();

        /// <summary>
        /// Method to process the requested horizontal resize.
        /// </summary>
        /// <param name="horizontalChange">The <see cref="ManipulationDeltaRoutedEventArgs.Cumulative"/> horizontal change amount from the start in device-independent pixels DIP.</param>
        /// <returns><see cref="bool"/> indicates if a change was made</returns>
        /// <remarks>
        /// The value provided here is the cumulative change from the beginning of the
        /// manipulation. This method will be used regardless of input device. It will already
        /// be adjusted for RightToLeft <see cref="FlowDirection"/> of the containing
        /// layout/settings. It will also already account for any settings such as
        /// <see cref="DragIncrement"/> or <see cref="KeyboardIncrement"/>. The implementer
        /// just needs to use the provided value to manipulate their baseline stored
        /// in <see cref="OnDragStarting"/> to provide the desired change.
        /// </remarks>
        protected abstract bool OnDragHorizontal(double horizontalChange);

        /// <summary>
        /// Method to process the requested vertical resize.
        /// </summary>
        /// <param name="verticalChange">The <see cref="ManipulationDeltaRoutedEventArgs.Cumulative"/> vertical change amount from the start in device-independent pixels DIP.</param>
        /// <returns><see cref="bool"/> indicates if a change was made</returns>
        /// <remarks>
        /// The value provided here is the cumulative change from the beginning of the
        /// manipulation. This method will be used regardless of input device. It will also
        /// already account for any settings such as <see cref="DragIncrement"/> or
        /// <see cref="KeyboardIncrement"/>. The implementer just needs
        /// to use the provided value to manipulate their baseline stored
        /// in <see cref="OnDragStarting"/> to provide the desired change.
        /// </remarks>
        protected abstract bool OnDragVertical(double verticalChange);

        /// <summary>
        /// Initializes a new instance of the <see cref="SizerBase"/> class.
        /// </summary>
        public SizerBase() {
            this.DefaultStyleKey = typeof(SizerBase);
        }

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        /// <returns>An automation peer for this <see cref="SizerBase"/>.</returns>
        protected override AutomationPeer OnCreateAutomationPeer() {
            return new SizerAutomationPeer(this);
        }


        /// <inheritdoc/>
        protected override void OnApplyTemplate() {
            base.OnApplyTemplate();

            // Unregister Events
            Loaded -= SizerBase_Loaded;
            PointerEntered -= SizerBase_PointerEntered;
            PointerExited -= SizerBase_PointerExited;
            PointerPressed -= SizerBase_PointerPressed;
            PointerReleased -= SizerBase_PointerReleased;
            ManipulationStarted -= SizerBase_ManipulationStarted;
            ManipulationCompleted -= SizerBase_ManipulationCompleted;
            IsEnabledChanged -= SizerBase_IsEnabledChanged;

            // Register Events
            Loaded += SizerBase_Loaded;
            PointerEntered += SizerBase_PointerEntered;
            PointerExited += SizerBase_PointerExited;
            PointerPressed += SizerBase_PointerPressed;
            PointerReleased += SizerBase_PointerReleased;
            ManipulationStarted += SizerBase_ManipulationStarted;
            ManipulationCompleted += SizerBase_ManipulationCompleted;
            IsEnabledChanged += SizerBase_IsEnabledChanged;

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7)) {
                Grid root = GetTemplateChild("RootGrid") as Grid;
                root.BackgroundTransition = new BrushTransition {
                    Duration = TimeSpan.FromMilliseconds(83)
                };
            }

            // Trigger initial state transition based on if we're Enabled or not currently.
            SizerBase_IsEnabledChanged(this, null);

            // Ensure we have the proper cursor value setup, as we can only set now for WinUI 3
            OnOrientationPropertyChanged(this, null);

            // Ensure we set the Thumb visiblity
            OnIsThumbVisiblePropertyChanged(this, null);
        }

        private void SizerBase_Loaded(object sender, RoutedEventArgs e) {
            Loaded -= SizerBase_Loaded;

            OnLoaded(e);
        }


        protected override void OnKeyDown(KeyRoutedEventArgs e) {
            // If we're manipulating with mouse/touch, we ignore keyboard inputs.
            if (_dragging) {
                return;
            }

            //// TODO: Do we want Ctrl/Shift to be a small increment (kind of inverse to old GridSplitter logic)?
            //// var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
            //// if (ctrl.HasFlag(CoreVirtualKeyStates.Down))
            //// Note: WPF doesn't do anything here.
            //// I think if we did anything, we'd create a SmallKeyboardIncrement property?

            // Initialize a drag event for this keyboard interaction.
            OnDragStarting();

            if (Orientation == Orientation.Vertical) {
                var horizontalChange = KeyboardIncrement;

                // Important: adjust for RTL language flow settings and invert horizontal axis
#if !HAS_UNO
                if (this.FlowDirection == FlowDirection.RightToLeft) {
                    horizontalChange *= -1;
                }
#endif

                if (e.Key == Windows.System.VirtualKey.Left) {
                    OnDragHorizontal(-horizontalChange);
                } else if (e.Key == Windows.System.VirtualKey.Right) {
                    OnDragHorizontal(horizontalChange);
                }
            } else {
                if (e.Key == Windows.System.VirtualKey.Up) {
                    OnDragVertical(-KeyboardIncrement);
                } else if (e.Key == Windows.System.VirtualKey.Down) {
                    OnDragVertical(KeyboardIncrement);
                }
            }
        }

        /// <inheritdoc />
        protected override void OnManipulationStarting(ManipulationStartingRoutedEventArgs e) {
            base.OnManipulationStarting(e);

            OnDragStarting();
        }

        /// <inheritdoc />
        protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e) {
            // We use Truncate here to provide 'snapping' points with the DragIncrement property
            // It works for both our negative and positive values, as otherwise we'd need to use
            // Ceiling when negative and Floor when positive to maintain the correct behavior.
            var horizontalChange =
                Math.Truncate(e.Cumulative.Translation.X / DragIncrement) * DragIncrement;
            var verticalChange =
                Math.Truncate(e.Cumulative.Translation.Y / DragIncrement) * DragIncrement;

            // Important: adjust for RTL language flow settings and invert horizontal axis
#if !HAS_UNO
            if (this.FlowDirection == FlowDirection.RightToLeft) {
                horizontalChange *= -1;
            }
#endif

            if (Orientation == Orientation.Vertical) {
                if (!OnDragHorizontal(horizontalChange)) {
                    return;
                }
            } else if (Orientation == Orientation.Horizontal) {
                if (!OnDragVertical(verticalChange)) {
                    return;
                }
            }

            base.OnManipulationDelta(e);
        }

        // private helper bools for Visual States
        private bool _pressed = false;
        private bool _dragging = false;
        private bool _pointerEntered = false;

        private void SizerBase_PointerReleased(object sender, PointerRoutedEventArgs e) {
            _pressed = false;

            if (IsEnabled) {
                VisualStateManager.GoToState(this, _pointerEntered ? PointerOverState : NormalState, true);
            }
        }

        private void SizerBase_PointerPressed(object sender, PointerRoutedEventArgs e) {
            _pressed = true;

            if (IsEnabled) {
                VisualStateManager.GoToState(this, PointerOverState, true);
            }
        }

        private void SizerBase_PointerExited(object sender, PointerRoutedEventArgs e) {
            _pointerEntered = false;

            if (!_pressed && !_dragging && IsEnabled) {
                VisualStateManager.GoToState(this, NormalState, true);
            }
        }

        private void SizerBase_PointerEntered(object sender, PointerRoutedEventArgs e) {
            _pointerEntered = true;

            if (!_pressed && !_dragging && IsEnabled) {
                VisualStateManager.GoToState(this, PointerOverState, true);
            }
        }

        private void SizerBase_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e) {
            _dragging = false;
            _pressed = false;
            VisualStateManager.GoToState(this, _pointerEntered ? PointerOverState : NormalState, true);
        }

        private void SizerBase_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e) {
            _dragging = true;
            VisualStateManager.GoToState(this, PressedState, true);
        }

        private void SizerBase_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (!IsEnabled) {
                VisualStateManager.GoToState(this, DisabledState, true);
            } else {
                VisualStateManager.GoToState(this, _pointerEntered ? PointerOverState : NormalState, true);
            }
        }


        /// <summary>
        /// Check for new requested vertical size is valid or not
        /// </summary>
        /// <param name="target">Target control being resized</param>
        /// <param name="newHeight">The requested new height</param>
        /// <param name="parentActualHeight">The parent control's ActualHeight</param>
        /// <returns>Bool result if requested vertical change is valid or not</returns>
        protected static bool IsValidHeight(FrameworkElement target, double newHeight, double parentActualHeight) {
            var minHeight = target.MinHeight;
            if (newHeight < 0 || (!double.IsNaN(minHeight) && newHeight < minHeight)) {
                return false;
            }

            var maxHeight = target.MaxHeight;
            if (!double.IsNaN(maxHeight) && newHeight > maxHeight) {
                return false;
            }

            if (newHeight <= parentActualHeight) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check for new requested horizontal size is valid or not
        /// </summary>
        /// <param name="target">Target control being resized</param>
        /// <param name="newWidth">The requested new width</param>
        /// <param name="parentActualWidth">The parent control's ActualWidth</param>
        /// <returns>Bool result if requested horizontal change is valid or not</returns>
        protected static bool IsValidWidth(FrameworkElement target, double newWidth, double parentActualWidth) {
            var minWidth = target.MinWidth;
            if (newWidth < 0 || (!double.IsNaN(minWidth) && newWidth < minWidth)) {
                return false;
            }

            var maxWidth = target.MaxWidth;
            if (!double.IsNaN(maxWidth) && newWidth > maxWidth) {
                return false;
            }

            if (newWidth <= parentActualWidth) {
                return false;
            }

            return true;
        }
    }
}