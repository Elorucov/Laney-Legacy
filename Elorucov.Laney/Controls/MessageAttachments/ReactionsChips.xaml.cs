using Elorucov.Laney.Models;
using Elorucov.Laney.Services.Common;
using Elorucov.VkAPI.Methods;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Elorucov.Laney.Controls.MessageAttachments {
    public sealed partial class ReactionsChips : UserControl {
        public static DependencyProperty IsOutgoingProperty = DependencyProperty.Register(nameof(IsOutgoing), typeof(bool), typeof(ReactionsChips), new PropertyMetadata(false));

        public bool IsOutgoing {
            get { return (bool)GetValue(IsOutgoingProperty); }
            set { SetValue(IsOutgoingProperty, value); }
        }

        public static DependencyProperty IsDarkAppearanceProperty = DependencyProperty.Register(nameof(IsDarkAppearance), typeof(bool), typeof(ReactionsChips), new PropertyMetadata(false));

        public bool IsDarkAppearance {
            get { return (bool)GetValue(IsDarkAppearanceProperty); }
            set { SetValue(IsDarkAppearanceProperty, value); }
        }

        public static DependencyProperty ReactionsProperty = DependencyProperty.Register(nameof(Reactions), typeof(ObservableCollection<Reaction>), typeof(ReactionsChips), new PropertyMetadata(false));

        public ObservableCollection<Reaction> Reactions {
            get { return (ObservableCollection<Reaction>)GetValue(ReactionsProperty); }
            set { SetValue(ReactionsProperty, value); }
        }

        public static DependencyProperty SelectedReactionIdProperty = DependencyProperty.Register(nameof(SelectedReactionId), typeof(int), typeof(ReactionsChips), new PropertyMetadata(-1));

        public int SelectedReactionId {
            get { return (int)GetValue(SelectedReactionIdProperty); }
            set { SetValue(SelectedReactionIdProperty, value); }
        }

        private long PeerId;
        private int ConvMsgId;

        public ReactionsChips(long peerId, int cmId) {
            this.InitializeComponent();
            PeerId = peerId;
            ConvMsgId = cmId;

            long oid = RegisterPropertyChangedCallback(IsOutgoingProperty, OnOutgoingPropertyChanged);
            long did = RegisterPropertyChangedCallback(IsDarkAppearanceProperty, OnDarkAppearancePropertyChanged);
            long sid = RegisterPropertyChangedCallback(SelectedReactionIdProperty, ChangeSelection);
            Loaded += ReactionsChips_Loaded;
            Unloaded += (a, b) => {
                UnregisterPropertyChangedCallback(IsOutgoingProperty, oid);
                UnregisterPropertyChangedCallback(IsDarkAppearanceProperty, did);
                UnregisterPropertyChangedCallback(SelectedReactionIdProperty, sid);
                Loaded -= ReactionsChips_Loaded;
            };
        }

        private void ReactionsChips_Loaded(object sender, RoutedEventArgs e) {
            ChangeSelection();
        }

        private void OnOutgoingPropertyChanged(DependencyObject sender, DependencyProperty dp) {
            ChipsGV.Margin = new Thickness(0, 0, IsOutgoing ? -4 : 0, -10);
            ChangeStyle();
        }

        private void OnDarkAppearancePropertyChanged(DependencyObject sender, DependencyProperty dp) {
            ChangeStyle();
        }

        private void ChangeStyle() {
            if (IsDarkAppearance) {
                ChipsGV.ItemContainerStyle = Resources["DarkGVIStyle"] as Style;
                return;
            }
            ChipsGV.ItemContainerStyle = Resources[IsOutgoing ? "OutgoingGVIStyle" : "IncomingGVIStyle"] as Style;
        }

        private void ChangeSelection(DependencyObject sender, DependencyProperty dp) {
            ChangeSelection();
        }

        private void ChangeSelection() {
            if (Reactions == null) return;
            Reaction selected = Reactions.Where(r => r.Id == SelectedReactionId).FirstOrDefault();
            ChipsGV.SelectedItem = selected;
        }

        private void Image_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) {
            Image image = sender as Image;
            if (args.NewValue == null) {
                image.Visibility = Visibility.Collapsed;
                image.Tag = args.NewValue;
                return;
            }

            Reaction reaction = args.NewValue as Reaction;
            if (image.Tag != null && image.Tag is Reaction or && reaction.Id == or.Id) return;
            image.Tag = args.NewValue;

            Uri reactionImage = reaction.ImagePath;
            if (reactionImage != null) image.Source = new SvgImageSource {
                UriSource = reactionImage,
            };
        }

        private void ChipsGV_ItemClick(object sender, ItemClickEventArgs e) {
            Reaction reaction = e.ClickedItem as Reaction;
            ChipsGV.IsEnabled = false;

            new System.Action(async () => {
                if (reaction.Id == SelectedReactionId) {
                    var response = await Messages.DeleteReaction(PeerId, ConvMsgId);
                    Functions.ShowHandledErrorTip(response);
                } else {
                    var response = await Messages.SendReaction(PeerId, ConvMsgId, reaction.Id);
                    Functions.ShowHandledErrorTip(response);
                }
            })();

            ChipsGV.IsEnabled = true;
        }
    }
}