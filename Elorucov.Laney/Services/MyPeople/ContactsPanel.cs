using Elorucov.Laney.Services.Common;
using Elorucov.Laney.Services.Logger;
using Elorucov.Laney.Services.Network;
using Elorucov.VkAPI.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Contact = Windows.ApplicationModel.Contacts.Contact;

namespace Elorucov.Laney.Services.MyPeople {
    public class ContactsPanel {
        static bool CanUseContactPanelInWin11() {
            return AppParameters.ContactPanelOnWin11Enabled || !Functions.IsWin11();
        }

        public static bool IsContactPanelSupported {
            get {
                return ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5)
                    && AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop"
                    && CanUseContactPanelInWin11();
            }
        }

        const string ContactListName = "Laney VK users";

        private static async Task<ContactList> GetContactListAsync() {
            ContactStore store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
            AppParameters.ContactsPanelInteracted = true;

            IReadOnlyList<ContactList> lists = await store.FindContactListsAsync();
            ContactList sampleList = null;
            foreach (ContactList list in lists) {
                if (list.DisplayName == ContactListName) {
                    sampleList = list;
                    break;
                }
            }

            if (sampleList == null) {
                sampleList = await store.CreateContactListAsync(ContactListName);
            }

            return sampleList;
        }

        public static async Task<Contact> GetContactAsync(long userid) {
            ContactList list = await GetContactListAsync();
            Contact contact = await list.GetContactFromRemoteIdAsync(userid.ToString());
            return contact;
        }

        public static async Task<Contact> GetContactAsync(User user) {
            return await GetContactAsync(user.Id);
        }

        private static async Task<Contact> CreateContactAsync(User user) {
            try {
                ContactList list = await GetContactListAsync();
                Contact contact = await list.GetContactFromRemoteIdAsync(user.Id.ToString());

                if (contact == null) {
                    StorageFile tempAvaFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync($"{user.Id}.jpg", CreationCollisionOption.GenerateUniqueName);
                    Stream fs = await tempAvaFile.OpenStreamForWriteAsync();
                    bool avaDownloadedSucess = false;

                    var resp = await LNet.GetAsync(user.Photo);
                    if (resp != null && resp.IsSuccessStatusCode) {
                        avaDownloadedSucess = true;
                        await resp.Content.CopyToAsync(fs);
                        fs.Seek(0, SeekOrigin.Begin);
                        fs.Flush();
                    }

                    contact = new Contact();
                    contact.RemoteId = user.Id.ToString();
                    contact.FirstName = user.FirstName;
                    contact.LastName = user.LastName;
                    contact.Nickname = user.ScreenName;
                    if (avaDownloadedSucess) contact.SourceDisplayPicture = RandomAccessStreamReference.CreateFromFile(tempAvaFile);
                    await list.SaveContactAsync(contact);
                }
                return contact;
            } catch (Exception ex) {
                Log.Error(ex, $"ContactsPanel > Cannot create a contact! (id: {user.Id}, name: {user.FullName})");
                return null;
            }
        }

        private static async Task<bool> AnnotateContactAsync(Contact contact) {
            ContactAnnotation contactAnnotation = new ContactAnnotation {
                ContactId = contact.Id,
                RemoteId = contact.RemoteId,
                SupportedOperations = ContactAnnotationOperations.ContactProfile | ContactAnnotationOperations.Share
            };

            var infos = await Windows.System.AppDiagnosticInfo.RequestInfoForAppAsync();
            contactAnnotation.ProviderProperties.Add("ContactPanelAppID", infos[0].AppInfo.AppUserModelId);
            contactAnnotation.ProviderProperties.Add("ContactShareAppID", infos[0].AppInfo.AppUserModelId);

            var contactAnnotationStore = await ContactManager.RequestAnnotationStoreAsync(ContactAnnotationStoreAccessType.AppAnnotationsReadWrite);
            var contactAnnotationLists = await contactAnnotationStore.FindAnnotationListsAsync();

            ContactAnnotationList contactAnnotationList = contactAnnotationLists.Count > 0 ? contactAnnotationLists[0] : null;

            if (contactAnnotationList == null) {
                contactAnnotationList = await contactAnnotationStore.CreateAnnotationListAsync();
            }


            return await contactAnnotationList.TrySaveAnnotationAsync(contactAnnotation);
        }

        public static async Task<string> GetRemoteIdForContactAsync(Contact fullContact) {
            var contactAnnotationStore = await ContactManager.RequestAnnotationStoreAsync(ContactAnnotationStoreAccessType.AppAnnotationsReadWrite);
            var contactAnnotations = await contactAnnotationStore.FindAnnotationsForContactAsync(fullContact);
            if (contactAnnotations.Count >= 0) {
                return contactAnnotations[0].RemoteId;
            }

            return string.Empty;
        }

        private static async Task<bool> PinContactAsync(Contact contact) {
            // Get the PinnedContactManager for the current user.
            PinnedContactManager pinnedContactManager = PinnedContactManager.GetDefault();

            // Check whether pinning to the taskbar is supported.
            if (!pinnedContactManager.IsPinSurfaceSupported(PinnedContactSurface.Taskbar)) {
                // If not, then there is nothing for this program to do.
                return false;
            }

            // Pin the contact to the taskbar.
            return await pinnedContactManager.RequestPinContactAsync(contact, PinnedContactSurface.Taskbar);
        }

        public static async Task<bool> PinUserAsync(User user) {
            Contact contact = await CreateContactAsync(user);
            if (contact != null) {
                bool r = await AnnotateContactAsync(contact);
                if (!r) return r;
                return await PinContactAsync(contact);
            } else {
                return false;
            }
        }

        public static async Task<bool> UnpinUserAsync(long id) {
            Contact contact = await GetContactAsync(id);
            PinnedContactManager pinnedContactManager = PinnedContactManager.GetDefault();
            return await pinnedContactManager.RequestUnpinContactAsync(contact, PinnedContactSurface.Taskbar);
        }

        public static async Task ClearAsync() {
            try {
                if (CanUseContactPanelInWin11() || !AppParameters.ContactsPanelInteracted) return; // Чтобы при выходе из аккаунта не требовало разрешение на доступ к контактам
                ContactStore store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);

                IReadOnlyList<ContactList> lists = await store.FindContactListsAsync();
                ContactList sampleList = null;
                foreach (ContactList list in lists) {
                    if (list.DisplayName == ContactListName) {
                        sampleList = list;
                        break;
                    }
                }

                if (sampleList != null) {
                    await sampleList.DeleteAsync();
                }
            } catch (Exception ex) {
                Log.Error($"ContactsPanel > Cannot clear contacts! 0x{ex.HResult.ToString("x8")}");
            }
        }

        public static async Task<bool> IsPinned(User user) {
            return await IsPinned(user.Id);
        }

        public static async Task<bool> IsPinned(long id) {
            try {
                Contact contact = await GetContactAsync(id);
                if (contact == null) return false;
                PinnedContactManager pinnedContactManager = PinnedContactManager.GetDefault();
                return pinnedContactManager.IsContactPinned(contact, PinnedContactSurface.Taskbar);
            } catch (Exception ex) {
                Log.Error($"Cannot check is contact pinned! 0x{ex.HResult.ToString("x8")}: {ex.Message}");
                return false;
            }
        }
    }
}