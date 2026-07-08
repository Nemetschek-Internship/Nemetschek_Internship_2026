namespace Services.Options;

public sealed class SmtpOptions
{
    private SmtpOptions(){}
    public SmtpOptions(string smtphost, int port, string username, string password, string email, string name)
    {
        Host = smtphost;
        Port = port;
        Username = username;
        Password = password;
        FromEmail = email;
        FromName = name;
    }
    public string Host { get; init; }
    public int Port { get; init; }
    public string Username { get; init; }
    public string Password { get; init; }
    public string? FromEmail { get; init; }
    public string? FromName { get; set; }
}