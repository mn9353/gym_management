namespace GymManagementBackend.Configuration
{
    public class EmailNotificationSettings
    {
        public bool Enabled { get; set; } = true;
        public string FromEmail { get; set; } = "onboarding@gymmanager9353.com";
        public string FromName { get; set; } = "Gym Manager";
        public string LoginUrl { get; set; } = "https://gymmanager9353.com/login";
        public string? BrandImageUrl { get; set; }
    }
}
