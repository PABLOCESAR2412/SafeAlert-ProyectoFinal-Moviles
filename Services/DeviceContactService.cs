namespace SafeAlert.Services;

public sealed record DeviceContact(string Name, string Phone);

public static class DeviceContactService
{
    public static async Task<DeviceContact?> PickAsync()
    {
#if ANDROID
        try
        {
            var granted = await PermissionService.EnsureContactsAsync();
            if (!granted)
                return null;

            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (activity == null)
                return null;

            var intent = new Android.Content.Intent(Android.Content.Intent.ActionPick);
            intent.SetType(Android.Provider.ContactsContract.CommonDataKinds.Phone.ContentType);

            var task = new TaskCompletionSource<DeviceContact?>(TaskCreationOptions.RunContinuationsAsynchronously);
            AndroidActivityResult.Register(intent, activity, (uri) =>
            {
                if (uri == null)
                {
                    task.TrySetResult(null);
                    return;
                }

                try
                {
                    var cursor = activity.ContentResolver?.Query(uri,
                        [Android.Provider.ContactsContract.Contacts.InterfaceConsts.DisplayName,
                         Android.Provider.ContactsContract.CommonDataKinds.Phone.Number],
                        null, null, null);

                    if (cursor != null)
                    {
                        using (cursor)
                        {
                            if (cursor.MoveToFirst())
                            {
                                var nameIndex = cursor.GetColumnIndex(Android.Provider.ContactsContract.Contacts.InterfaceConsts.DisplayName);
                                var phoneIndex = cursor.GetColumnIndex(Android.Provider.ContactsContract.CommonDataKinds.Phone.Number);
                                var name = nameIndex >= 0 ? cursor.GetString(nameIndex) ?? "" : "";
                                var phone = phoneIndex >= 0 ? cursor.GetString(phoneIndex) ?? "" : "";
                                task.TrySetResult(new DeviceContact(name.Trim(), phone.Trim()));
                                return;
                            }
                        }
                    }
                }
                catch
                {
                }

                task.TrySetResult(null);
            });

            return await task.Task;
        }
        catch
        {
            return null;
        }
#else
        await Task.CompletedTask;
        return null;
#endif
    }
}
