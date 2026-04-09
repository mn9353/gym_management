namespace GymManagementBackend.Configuration
{
    public class MembershipStatusJobSettings
    {
        public bool Enabled { get; set; } = true;
        public int RunAtHour24 { get; set; } = 9;
        public int RunAtMinute { get; set; } = 0;
        public string TimeZoneId { get; set; } = "Asia/Kolkata";
    }
}
