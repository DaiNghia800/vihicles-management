namespace Public_Transport.Helpers
{
    public static class WebConstants
    {
        // Roles
        public const string ROLE_ADMIN = "Admin";
        public const string ROLE_PASSENGER = "Passenger"; // ✅ Đổi từ ROLE_CUSTOMER
        public const string ROLE_DRIVER = "Driver";
        
        // Policies
        public const string POLICY_NO_PASSENGER = "NoPassenger"; // ✅ Đổi từ NoCustomer
        
        // Other constants
        public const string SUCCESS = "Success";
        public const string ERROR = "Error";
        public const string DEFAULT_AVATAR = "[\"https://res.cloudinary.com/dfeaar87r/image/upload/v1763101391/default-avatar_uek2f1.png\"]";
    }
}
