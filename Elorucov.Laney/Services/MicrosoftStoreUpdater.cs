// https://github.com/jenius-apps/common/blob/main/src/JeniusApps.Common.Uwp/Tools/MicrosoftStoreUpdater.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Services.Store;

namespace Elorucov.Laney.Services {
    /// <summary>
    /// Class for updating the app using Microsoft Store SDK.
    /// </summary>
    public sealed class MicrosoftStoreUpdater {
        private StoreContext _context;
        private IReadOnlyList<StorePackageUpdate> _updates = null;

        public event EventHandler<double> ProgressChanged;
        public event EventHandler UpdateAvailable;
        public event EventHandler Downloaded;

        public async Task<bool> TrySilentDownloadAsync() {
            try {
                var hasUpdates = await CheckForUpdatesAsync();

                if (!hasUpdates || !_context.CanSilentlyDownloadStorePackageUpdates) {
                    return false;
                }

                StorePackageUpdateResult downloadResult = await _context.TrySilentDownloadStorePackageUpdatesAsync(_updates);
                if (downloadResult.OverallState is StorePackageUpdateState.Completed) {
                    Downloaded?.Invoke(this, EventArgs.Empty);
                    return true;
                }
                return false;
            } catch (Exception) { // Strange crash on winmobile...
                return false;
            }
        }

        public async Task<bool> CheckForUpdatesAsync() {
            if (_context == null) _context = StoreContext.GetDefault();

            try {
                _updates = await _context.GetAppAndOptionalStorePackageUpdatesAsync();
            } catch (FileNotFoundException) {
                // This exception occurs if the app is not associated with the store.
                return false;
            } catch (Exception) {
                // Another exceptions occurs in LTSC builds of Windows 10/11.
                return false;
            }

            if (_updates is null) {
                return false;
            }

            if (_updates.Count > 0) {
                UpdateAvailable?.Invoke(this, EventArgs.Empty);
            }

            return _updates.Count > 0;
        }

        public async Task<bool?> TryApplyUpdatesAsync() {
            if (_updates is null || _updates.Count == 0) {
                return null;
            }

            if (_context == null) _context = StoreContext.GetDefault();

            IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> downloadOperation =
                    _context.RequestDownloadAndInstallStorePackageUpdatesAsync(_updates);

            downloadOperation.Progress = (asyncInfo, progress) => {
                ProgressChanged?.Invoke(null, progress.PackageDownloadProgress);
            };

            var result = await downloadOperation.AsTask();

            return result.OverallState is StorePackageUpdateState.Completed;
        }
    }
}