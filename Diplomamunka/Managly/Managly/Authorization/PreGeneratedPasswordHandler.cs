using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Threading.Tasks;
using Managly.Models;

public class PreGeneratedPasswordHandler : AuthorizationHandler<PreGeneratedPasswordRequirement>
{
    private readonly UserManager<User> _userManager;

    public PreGeneratedPasswordHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PreGeneratedPasswordRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user != null && user.IsUsingPreGeneratedPassword)
        {
            context.Succeed(requirement); 
        }
    }
}
