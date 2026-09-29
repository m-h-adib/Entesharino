namespace Entesharino.Application.Common.Interfaces;

public interface ISecretProtector
{
    string Protect(string value);
    string Unprotect(string value);
}
