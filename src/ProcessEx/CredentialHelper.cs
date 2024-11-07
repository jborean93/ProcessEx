namespace ProcessEx;

internal static class CredentialHelper
{
    public static (string, string?) SplitUserName(string username)
    {
        string? domain = null;
        if (username.IndexOf('\\') != -1)
        {
            string[] userSplit = username.Split(['\\'], 2);
            domain = userSplit[0];
            username = userSplit[1];
        }

        return (username, domain);
    }
}
