using Microsoft.EntityFrameworkCore;
using Acme.BookStore.Users;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Xunit;
using Acme.BookStore.Books;
using Acme.BookStore.Authors;
using Volo.Abp.Security.Claims;
using Volo.Abp.Data;
using System.Security.Claims;
using Acme.BookStore.Security;

namespace Acme.BookStore.EntityFrameworkCore.Samples
{
  /* This is just an example test class.
   * Normally, you don't test ABP framework code
   * (like default AppUser repository IRepository<AppUser, Guid> here).
   * Only test your custom repository methods.
   */
  public class AuthorRepositoryTests : BookStoreEntityFrameworkCoreTestBase
  {
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;
    private readonly IAuthorRepository _authorRepository;
    private readonly AuthorManager _authorManager;

    public AuthorRepositoryTests()
    {
      _authorRepository = GetRequiredService<IAuthorRepository>();
      _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
      _authorManager = GetRequiredService<AuthorManager>();
    }

    [Fact]
    public async Task Concurency_Author()
    {
      Guid User2ClaimUserId = Guid.Parse(Consts.User2ClaimUserId);
      Guid adminClaimUserId = Guid.Parse(Consts.AdminClaimUserId);

      ChangeCurrentPrincipal(Consts.User2ClaimUserId);
      Author autorFromCreate = await CreateAuthorAsync();

      Author authorFromUsrUser2 = await _authorRepository.GetAsync(autorFromCreate.Id);

      ChangeCurrentPrincipal(Consts.AdminClaimUserId);
      Author authorFromUsrAdmin = await _authorRepository.GetAsync(autorFromCreate.Id);

      await _authorManager.ChangeNameAsync(authorFromUsrAdmin, "UsrAdmin");

      System.Threading.Thread.Sleep(1000); 

      await WithUnitOfWorkAsync(async () =>
      {
        _ = await _authorRepository.UpdateAsync(authorFromUsrAdmin, autoSave: true);

        Author authorAfterUpdateUserAdmin = await _authorRepository.GetAsync(autorFromCreate.Id);
        authorAfterUpdateUserAdmin.Name.ShouldBe("UsrAdmin");
        authorAfterUpdateUserAdmin.ConcurrencyStamp.ShouldNotBe(autorFromCreate.ConcurrencyStamp);
        authorAfterUpdateUserAdmin.LastModifierId?.ShouldBe(adminClaimUserId);
      });

      ChangeCurrentPrincipal(Consts.User2ClaimUserId);
      await _authorManager.ChangeNameAsync(authorFromUsrUser2, "UsrUser2");

      System.Threading.Thread.Sleep(1000); 

      _ = await Assert.ThrowsAnyAsync<AbpDbConcurrencyException>(async () =>
      {
        await WithUnitOfWorkAsync(async () =>
        {
          _ = await _authorRepository.UpdateAsync(authorFromUsrUser2, autoSave: true);
        });
      });


    }

    private async Task<Author> CreateAuthorAsync()
    {
      Author author = await _authorManager.CreateAsync(
          Guid.NewGuid().ToString(),
          new DateTime(1919, 1, 19),
          "ShortBio"
      );

      await WithUnitOfWorkAsync(async () =>
      {
        await _authorRepository.InsertAsync(author, autoSave: true);
      });

      return author;
    }

    private void ChangeCurrentPrincipal(string userId)
    {
      ClaimsPrincipal newPrincipal;
      if (userId == Consts.AdminClaimUserId)
      {
        newPrincipal = FakeCurrentPrincipal.AdminClaimsPrincipal();
      }
      else
      {
        newPrincipal = FakeCurrentPrincipal.User2ClaimsPrincipal();
      }

      _ = _currentPrincipalAccessor!.Change(newPrincipal);
    }

  }
}
