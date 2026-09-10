using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Users.ResetPassword;

public sealed record ResetUserPasswordCommand(Guid Id) : ICommand<bool>;
