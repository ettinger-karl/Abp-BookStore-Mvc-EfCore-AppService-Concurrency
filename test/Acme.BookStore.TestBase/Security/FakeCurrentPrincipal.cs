using System.Collections.Generic;
using System.Security.Claims;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Security.Claims;

namespace Acme.BookStore.Security
{
  static public class FakeCurrentPrincipal
  {
    public static ClaimsPrincipal User2ClaimsPrincipal()
    {
      return new(
        new ClaimsIdentity(
          new Claim[]
          {
            new Claim(AbpClaimTypes.UserId, Consts.User2ClaimUserId),
            new Claim(AbpClaimTypes.UserName, Consts.User2UserName),
            new Claim(AbpClaimTypes.Email, Consts.User2Email)
          }
        )
      );
    }

    public static ClaimsPrincipal AdminClaimsPrincipal()
    {
      return new(
        new ClaimsIdentity(
          new Claim[]
          {
            new Claim(AbpClaimTypes.UserId, Consts.AdminClaimUserId),
            new Claim(AbpClaimTypes.UserName, Consts.AdminUserName),
            new Claim(AbpClaimTypes.Email, Consts.AdminEmail)
          }
        )
      );
    }
  }
}
