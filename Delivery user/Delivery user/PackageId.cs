using Delivery_user.Models;

namespace Delivery_user
{
    public class PackageId
    {
        public static class PackageIdGenerator
        {
            private static readonly Random random = new Random();

            public static string GenerateUniqueId(Context db)
            {
                string newId;
                do
                {
                    string datePart = DateTime.Now.ToString("yyyyMMdd");
                    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                    string randomPart = new string(Enumerable.Repeat(chars, 4)
                        .Select(s => s[random.Next(s.Length)]).ToArray());
                    newId = $"PKG{datePart}{randomPart}";
                }
                while (db.Packages.Any(p => p.PID == newId));

                return newId;
            }
        }
    }
}

