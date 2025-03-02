using System.ComponentModel.DataAnnotations;
using CitizenRequest.Models;

namespace CitizenRequest.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this ApplicationStatus status)
        {
            var type = status.GetType();
            var memberInfo = type.GetMember(status.ToString());
            var attributes = memberInfo[0].GetCustomAttributes(typeof(DisplayAttribute), false);
            return ((DisplayAttribute)attributes[0]).Name;
        }

        public static string GetBadgeClass(this ApplicationStatus status)
        {
            return status switch
            {
                ApplicationStatus.New => "primary",
                ApplicationStatus.InProgress => "warning",
                ApplicationStatus.Resolved => "success",
                ApplicationStatus.Rejected => "danger",
                _ => "secondary"
            };
        }
    }
}