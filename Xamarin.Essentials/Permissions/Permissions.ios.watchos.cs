﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using CoreLocation;
using CoreMotion;
using EventKit;
using Foundation;

namespace Xamarin.Essentials
{
    public static partial class Permissions
    {
        internal static class EventPermission
        {
            internal static PermissionStatus CheckPermissionStatus(EKEntityType entityType)
            {
                var status = EKEventStore.GetAuthorizationStatus(entityType);
                switch (status)
                {
                    case EKAuthorizationStatus.Authorized:
                        return PermissionStatus.Granted;
                    case EKAuthorizationStatus.Denied:
                        return PermissionStatus.Denied;
                    case EKAuthorizationStatus.Restricted:
                        return PermissionStatus.Restricted;
                    default:
                        return PermissionStatus.Unknown;
                }
            }

            internal static async Task<PermissionStatus> RequestPermissionAsync(EKEntityType entityType)
            {
                var eventStore = new EKEventStore();

                var results = await eventStore.RequestAccessAsync(entityType);

                return results.Item1 ? PermissionStatus.Granted : PermissionStatus.Denied;
            }
        }

        public partial class CalendarRead : IosBasePermission
        {
            protected override Func<IEnumerable<string>> RequiredInfoPlistKeys =>
                () => new string[] { "NSCalendarsUsageDescription" };

            public override Task<PermissionStatus> CheckStatusAsync()
            {
                EnsureDeclared();

                return Task.FromResult(EventPermission.CheckPermissionStatus(EKEntityType.Event));
            }

            public override Task<PermissionStatus> RequestAsync()
            {
                EnsureDeclared();

                var status = EventPermission.CheckPermissionStatus(EKEntityType.Event);
                if (status == PermissionStatus.Granted)
                    return Task.FromResult(status);

                return EventPermission.RequestPermissionAsync(EKEntityType.Event);
            }
        }

        public partial class CalendarWrite : CalendarRead
        {
        }

        public partial class Reminders : IosBasePermission
        {
            protected override Func<IEnumerable<string>> RequiredInfoPlistKeys =>
                () => new string[] { "NSRemindersUsageDescription" };

            public override Task<PermissionStatus> CheckStatusAsync()
            {
                EnsureDeclared();

                return Task.FromResult(EventPermission.CheckPermissionStatus(EKEntityType.Reminder));
            }

            public override Task<PermissionStatus> RequestAsync()
            {
                EnsureDeclared();

                var status = EventPermission.CheckPermissionStatus(EKEntityType.Reminder);
                if (status == PermissionStatus.Granted)
                    return Task.FromResult(status);

                return EventPermission.RequestPermissionAsync(EKEntityType.Reminder);
            }
        }

        public partial class Sensors : IosBasePermission
        {
            protected override Func<IEnumerable<string>> RequiredInfoPlistKeys =>
                () => new string[] { "NSMotionUsageDescription" };

            public override Task<PermissionStatus> CheckStatusAsync()
            {
                EnsureDeclared();

                return Task.FromResult(GetSensorPermissionStatus());
            }

            public override Task<PermissionStatus> RequestAsync()
            {
                EnsureDeclared();

                var status = GetSensorPermissionStatus();
                if (status == PermissionStatus.Granted)
                    return Task.FromResult(status);

                EnsureMainThread();

                return RequestSensorPermission();
            }

            internal static PermissionStatus GetSensorPermissionStatus()
            {
                // Check if it's available
                if (!CMMotionActivityManager.IsActivityAvailable)
                    return PermissionStatus.Disabled;

                if (Platform.HasOSVersion(11, 0))
                {
                    switch (CMMotionActivityManager.AuthorizationStatus)
                    {
                        case CMAuthorizationStatus.Authorized:
                            return PermissionStatus.Granted;
                        case CMAuthorizationStatus.Denied:
                            return PermissionStatus.Denied;
                        case CMAuthorizationStatus.NotDetermined:
                            return PermissionStatus.Unknown;
                        case CMAuthorizationStatus.Restricted:
                            return PermissionStatus.Restricted;
                    }
                }

                return PermissionStatus.Unknown;
            }

            internal static async Task<PermissionStatus> RequestSensorPermission()
            {
                var activityManager = new CMMotionActivityManager();

                try
                {
                    var results = await activityManager.QueryActivityAsync(NSDate.DistantPast, NSDate.DistantFuture, NSOperationQueue.MainQueue);
                    if (results != null)
                        return PermissionStatus.Granted;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Unable to query activity manager: " + ex.Message);
                    return PermissionStatus.Denied;
                }

                return PermissionStatus.Unknown;
            }
        }

        public partial class LocationAlways : IosBasePermission
        {
            protected override Func<IEnumerable<string>> RequiredInfoPlistKeys =>
                () => new string[]
                {
                    "NSLocationAlwaysAndWhenInUseUsageDescription",
                    "NSLocationAlwaysUsageDescription"
                };

            public override Task<PermissionStatus> CheckStatusAsync()
            {
                EnsureDeclared();

                return Task.FromResult(LocationWhenInUse.GetLocationStatus(false));
            }

            public override async Task<PermissionStatus> RequestAsync()
            {
                EnsureDeclared();

                var status = LocationWhenInUse.GetLocationStatus(false);
                if (status == PermissionStatus.Granted)
                    return status;

                EnsureMainThread();

                return await LocationWhenInUse.RequestLocationAsync(false, lm => lm.RequestAlwaysAuthorization());
            }
        }
    }
}