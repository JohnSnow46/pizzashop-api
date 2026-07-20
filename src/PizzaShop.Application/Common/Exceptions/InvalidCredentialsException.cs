namespace PizzaShop.Application.Common.Exceptions;

/// <summary>
/// Thrown when a login attempt fails — unknown email, wrong password, or a deactivated
/// account are all reported identically ("invalid credentials"), so a client can never probe
/// whether a given email is registered (ADR-0026 section 2.7).
///
/// api-layer.md leaves the exact HTTP mapping ambiguous between two places: section 2.4 says
/// "ForbiddenOperationException lub dedykowany 401", while section 2.7 is explicit that a
/// failed login "zwraca jednolity błąd 'invalid credentials' (401)". This type resolves that
/// ambiguity in favor of the more specific 2.7 wording: a dedicated exception mapped to
/// <b>401 Unauthorized</b> in the Api middleware. Reusing <see cref="ForbiddenOperationException"/>
/// (403) would be semantically wrong here — 403 means "you are identified, but not allowed to
/// do this"; a failed login means "you were never authenticated in the first place", which is
/// squarely 401. The message is safe to return to the client (no detail on which check failed).
/// </summary>
public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Invalid email or password.")
    {
    }
}
