using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Application.Common.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        string HashToken(string token);
        string GenerateEmailConfirmationToken(User user);
        Guid? ValidateEmailConfirmationToken(string token);
    }
}
