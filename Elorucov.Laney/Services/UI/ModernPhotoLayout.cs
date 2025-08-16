using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace Elorucov.Laney.Services.UI {
    public static class ModernPhotoLayout {
        public const double GAP = 2;

        public static Size[][] GeneratePhotoLayout(List<IPreview> previews, double initialMaxWidth, double initialMaxHeight, out double actualWidth) {
            Size[] layoutItems = new Size[previews.Count];
            int i = 0;
            foreach (IPreview preview in previews) {
                (double width, double height) = ResizeToMatchMinDimensions(preview.PreviewImageSize, 64);
                layoutItems[i] = new Size(width, height);
                i++;
            }

            var layout = GroupPhotosIntoRows(layoutItems);
            int rowsCount = layout.Length;
            double maxHeight = initialMaxHeight - GAP * (rowsCount - 1);
            double totalHeight = 0;

            double maxTotalWidth = layout.Aggregate(0d, (maxWidth, row) => {
                double totalWidth = row.Aggregate(0d, (width, photo) => width + photo.Width);
                return totalWidth > maxWidth ? totalWidth : maxWidth;
            });

            for (int j = 0; j < layout.Length; j++) {
                var row = layout[j];
                int photosInRowCount = row.Length;
                double maxWidth = initialMaxWidth - GAP * (photosInRowCount - 1);

                double rowAspectRatio = row.Aggregate(0d, (ratio, photo) => ratio + (photo.Width / photo.Height));
                double rowHeight = Math.Min(maxTotalWidth, maxWidth) / rowAspectRatio;
                totalHeight += rowHeight;

                double rightestPoint = 0;
                for (int k = 0; k < row.Length; k++) {
                    var photo = row[k];
                    double rwidth = Math.Ceiling((photo.Width / photo.Height) * rowHeight);
                    double rheight = Math.Ceiling(rowHeight);
                    rightestPoint += k == 0 ? rwidth : rwidth + GAP;

                    if (k == row.Length - 1) {
                        if (rightestPoint > initialMaxWidth) {
                            rwidth -= rightestPoint - initialMaxWidth;
                        }
                        rightestPoint = 0;
                    }

                    layout[j][k] = new Size(rwidth, rheight);
                }
            }

            double fitToMaxHeightFactor = maxHeight / totalHeight;
            if (fitToMaxHeightFactor < 1) {
                double maxWidth = Math.Ceiling(initialMaxWidth * fitToMaxHeightFactor);
                for (int j = 0; j < layout.Length; j++) {
                    var row = layout[j];
                    double rightestPoint = 0;
                    for (int k = 0; k < row.Length; k++) {
                        var photo = row[k];
                        double rwidth = Math.Ceiling(photo.Width * fitToMaxHeightFactor);
                        double rheight = Math.Ceiling(photo.Height * fitToMaxHeightFactor);

                        rightestPoint += k == 0 ? rwidth : rwidth + GAP;

                        if (k == row.Length - 1) {
                            if (rightestPoint > maxWidth) {
                                rwidth -= rightestPoint - maxWidth;
                            }
                            rightestPoint = 0;
                        }

                        layout[j][k] = new Size(rwidth, rheight);
                    }
                }
                actualWidth = maxWidth;
            } else {
                actualWidth = initialMaxWidth;
            }

            return layout;
        }

        private static Size[][] GroupPhotosIntoRows(Size[] layoutItems) {
            int count = layoutItems.Length;
            bool preferVerticalPositioning = layoutItems.Aggregate(0d, (ratio, photo) => ratio + photo.Width / photo.Height) / count > 1.2;

            // [[1], ([2]), ([3])]
            // or
            // [[1, (2), (3)]]
            if (count <= 3) {
                return preferVerticalPositioning ? layoutItems.Select(p => new Size[1] { p }).ToArray() : new Size[][] { layoutItems };
            }

            // [[1, 2], [3, 4]]
            if (count == 4) {
                return new Size[][] { layoutItems.Take(2).ToArray(), layoutItems.Skip(2).ToArray() };
            }

            // [[1], [2, 3], [4, 5]]
            // or
            // [[1, 2], [3, 4, 5]]
            if (count == 5) {
                return preferVerticalPositioning ?
                    new Size[][] { layoutItems.Take(1).ToArray(), layoutItems.Skip(1).Take(2).ToArray(), layoutItems.Skip(3).ToArray() } :
                    new Size[][] { layoutItems.Take(2).ToArray(), layoutItems.Skip(2).ToArray() };
            }

            // [[1, 2], [3, 4], [5, 6]]
            // or
            // [[1, 2, 3], [4, 5, 6]]
            if (count == 6) {
                return preferVerticalPositioning ?
                    new Size[][] { layoutItems.Take(2).ToArray(), layoutItems.Skip(2).Take(2).ToArray(), layoutItems.Skip(4).ToArray() } :
                    new Size[][] { layoutItems.Take(3).ToArray(), layoutItems.Skip(3).ToArray() };
            }

            // [[1, 2], [3, 4], [5, 6, 7]]
            if (count == 7) {
                return new Size[][] { layoutItems.Take(2).ToArray(), layoutItems.Skip(2).Take(2).ToArray(), layoutItems.Skip(4).ToArray() };
            }

            // [[1, 2], [3, 4, 5], [6, 7, 8]
            if (count == 8) {
                return new Size[][] { layoutItems.Take(2).ToArray(), layoutItems.Skip(2).Take(3).ToArray(), layoutItems.Skip(5).ToArray() };
            }

            // [[1, 2, 3], [4, 5, 6], [7, 8, 9, (10)]]
            return new Size[][] { layoutItems.Take(3).ToArray(), layoutItems.Skip(3).Take(3).ToArray(), layoutItems.Skip(6).ToArray() };
        }

        // width,  height
        private static (double, double) ResizeToMatchMinDimensions(Size size, double minDimension) {
            double width = size.Width;
            double height = size.Height;

            double scaleX = minDimension / width;
            double scaleY = minDimension / height;

            if (width < minDimension && height < minDimension) {
                double scale = Math.Max(scaleX, scaleY);
                return (width * scale, height * scale);
            }

            if (width < minDimension) {
                return (width * scaleX, height * scaleX);
            }

            if (height < minDimension) {
                return (width * scaleY, height * scaleY);
            }

            return (width, height);
        }
    }
}
