namespace lrfdev.auth.api.Security;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool Verify(string password, string storedHash);
}
