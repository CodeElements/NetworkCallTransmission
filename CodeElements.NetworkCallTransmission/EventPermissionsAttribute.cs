using System;

namespace CodeElements.NetworkCallTransmission
{
    /// <summary>
    ///     Define permissions for an event
    /// </summary>
    [AttributeUsage(AttributeTargets.Event)]
    public class EventPermissionsAttribute : Attribute
    {
        /// <summary>
        ///     Initialize a new instance of <see cref="EventPermissionsAttribute" />
        /// </summary>
        /// <param name="requiredPermissions">The permission ids</param>
        public EventPermissionsAttribute(params int[] requiredPermissions)
        {
            RequiredPermissions = requiredPermissions;
        }

        /// <summary>
        ///     The permission ids
        /// </summary>
        public int[] RequiredPermissions { get; set; }
    }
}