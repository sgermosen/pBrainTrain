using Android.App;
using Android.Content;
using Android.OS;
using BrainTrain.App.Core;
using AApplication = Android.App.Application;

namespace BrainTrain.App.Platforms.Android;

/// <summary>
/// Recordatorio diario con AlarmManager nativo (sin dependencias externas).
/// Usa alarmas inexactas repetitivas: suficientes para un hábito diario y sin
/// permisos especiales de alarma exacta.
/// </summary>
public sealed class AndroidReminderScheduler : IReminderScheduler
{
    internal const int RequestCode = 4801;

    public async Task<bool> RequestPermissionAsync()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.PostNotifications>();
            return status == PermissionStatus.Granted;
        }
        return true;
    }

    public Task ScheduleDailyAsync(TimeSpan localTime, string title, string message)
    {
        var context = AApplication.Context;
        var alarm = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (alarm is null) return Task.CompletedTask;

        var now = DateTime.Now;
        var first = now.Date + localTime;
        if (first <= now) first = first.AddDays(1);
        var triggerAt = new DateTimeOffset(first).ToUnixTimeMilliseconds();

        alarm.SetInexactRepeating(AlarmType.RtcWakeup, triggerAt, AlarmManager.IntervalDay, BuildIntent(title, message));

        // Persistimos para reprogramar tras un reinicio del teléfono.
        Preferences.Set("bt.reminder.title", title);
        Preferences.Set("bt.reminder.message", message);
        return Task.CompletedTask;
    }

    public Task CancelAllAsync()
    {
        var context = AApplication.Context;
        var alarm = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        alarm?.Cancel(BuildIntent("", ""));
        return Task.CompletedTask;
    }

    private static PendingIntent BuildIntent(string title, string message)
    {
        var context = AApplication.Context;
        var intent = new Intent(context, typeof(ReminderReceiver));
        intent.PutExtra("title", title);
        intent.PutExtra("message", message);
        return PendingIntent.GetBroadcast(context, RequestCode, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
    }
}

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class ReminderReceiver : BroadcastReceiver
{
    private const string ChannelId = "braintrain.daily";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null) return;
        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (manager is null) return;

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            var channel = new NotificationChannel(ChannelId, "Reto diario", NotificationImportance.Default)
            {
                Description = "Recordatorio del reto diario de BrainTrain"
            };
            manager.CreateNotificationChannel(channel);
        }

        var launch = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName ?? "");
        var contentIntent = launch is null ? null : PendingIntent.GetActivity(
            context, 0, launch, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var builder = (OperatingSystem.IsAndroidVersionAtLeast(26)
                ? new Notification.Builder(context, ChannelId)
#pragma warning disable CA1422 // ruta legacy para API < 26
                : new Notification.Builder(context))
#pragma warning restore CA1422
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetContentTitle(intent?.GetStringExtra("title") ?? "🧠 Tu reto diario te espera")
            .SetContentText(intent?.GetStringExtra("message") ?? "5 minutos de ingenio mantienen tu racha viva.")
            .SetAutoCancel(true);
        if (contentIntent is not null)
            builder.SetContentIntent(contentIntent);

        manager.Notify(AndroidReminderScheduler.RequestCode, builder.Build()!);
    }
}

/// <summary>Reprograma el recordatorio después de un reinicio del dispositivo.</summary>
[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter([Intent.ActionBootCompleted])]
public sealed class BootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action != Intent.ActionBootCompleted) return;
        if (Preferences.Get(BrainTrain.App.Core.ViewModels.SettingsViewModel.ReminderEnabledKey, "0") != "1") return;

        if (TimeSpan.TryParse(Preferences.Get(BrainTrain.App.Core.ViewModels.SettingsViewModel.ReminderHourKey, "19:00"), out var time))
        {
            _ = new AndroidReminderScheduler().ScheduleDailyAsync(
                time,
                Preferences.Get("bt.reminder.title", "🧠 Tu reto diario te espera"),
                Preferences.Get("bt.reminder.message", "5 minutos de ingenio mantienen tu racha viva."));
        }
    }
}
