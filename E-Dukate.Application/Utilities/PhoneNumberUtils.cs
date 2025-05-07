namespace E_Dukate.Application.Utilities;

public static class PhoneNumberUtils
{
    public static string NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber)) return "";
        var cleaned = phoneNumber.Replace("+", "").Replace(" ", "").Replace("-", "").Trim();
        if (cleaned.StartsWith("591") && cleaned.Length > 3)
        {
            cleaned = cleaned.Substring(3);
        }
        return cleaned;
    }
}