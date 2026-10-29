using Microsoft.AspNetCore.Identity;

namespace HelpDeskMvc.Data;

// Traduz para português as mensagens de erro padrão do Identity
// (que por padrão vêm em inglês) exibidas no cadastro.
public class IdentityErrorDescriberPtBr : IdentityErrorDescriber
{
    public override IdentityError DefaultError() =>
        new() { Code = nameof(DefaultError), Description = "Ocorreu um erro desconhecido." };

    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = $"O e-mail '{email}' já está em uso." };

    // Como o UserName é o próprio e-mail, a mensagem é a mesma do e-mail duplicado.
    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = $"O e-mail '{userName}' já está em uso." };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = $"O e-mail '{email}' é inválido." };

    public override IdentityError InvalidUserName(string? userName) =>
        new() { Code = nameof(InvalidUserName), Description = $"O e-mail '{userName}' contém caracteres inválidos." };

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"A senha deve ter pelo menos {length} caracteres." };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "A senha deve ter pelo menos um número (0-9)." };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "A senha deve ter pelo menos uma letra minúscula (a-z)." };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = "A senha deve ter pelo menos uma letra maiúscula (A-Z)." };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "A senha deve ter pelo menos um caractere especial." };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"A senha deve ter pelo menos {uniqueChars} caracteres diferentes." };

    public override IdentityError UserAlreadyInRole(string role) =>
        new() { Code = nameof(UserAlreadyInRole), Description = $"O usuário já possui o perfil '{role}'." };

    public override IdentityError UserNotInRole(string role) =>
        new() { Code = nameof(UserNotInRole), Description = $"O usuário não possui o perfil '{role}'." };

    public override IdentityError ConcurrencyFailure() =>
        new() { Code = nameof(ConcurrencyFailure), Description = "Os dados foram alterados por outra pessoa. Tente novamente." };
}
