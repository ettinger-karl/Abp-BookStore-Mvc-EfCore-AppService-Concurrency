using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Acme.BookStore.Security;
using Shouldly;
using Volo.Abp.Data;
using Volo.Abp.Security.Claims;
using Xunit;

namespace Acme.BookStore.Authors
{
  public class AuthorAppServiceConcurrency_Tests : BookStoreApplicationTestBase
  {
    private readonly IAuthorAppService _authorAppService;
    private readonly IAuthorRepository _authorRepository;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;
    private readonly AuthorManager _authorManager;

    public AuthorAppServiceConcurrency_Tests()
    {
      _authorAppService = GetRequiredService<IAuthorAppService>();
      _authorRepository = GetRequiredService<IAuthorRepository>();
      _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
      _authorManager = GetRequiredService<AuthorManager>();
  }

    [Fact]
    public async Task AppService_Concurrency_Failure()
    {
      AuthorDto resultAuthorFromCreate = await NewAuthorAsync();

      string createConcurrencyStamp = default;
      DateTime createCreationTime = default;
      DateTime? createLastModificationTime = default;
      Guid? createCreatorId = default;
      Guid? createLastModifierId = default;

      await WithUnitOfWorkAsync(async () =>
      {
        Author authorEntityFormCreate = await _authorRepository.GetAsync(resultAuthorFromCreate.Id);

        createConcurrencyStamp = authorEntityFormCreate.ConcurrencyStamp;
        createCreationTime = authorEntityFormCreate.CreationTime;
        createCreatorId = authorEntityFormCreate.CreatorId;
        createLastModificationTime = authorEntityFormCreate.LastModificationTime;
        createLastModifierId = authorEntityFormCreate.LastModifierId;

      });

      ChangeCurrentPrincipal(Consts.User2ClaimUserId);
      AuthorDto getAuthorFromUser2 = await _authorAppService.GetAsync(resultAuthorFromCreate.Id);

      ChangeCurrentPrincipal(Consts.AdminClaimUserId);
      AuthorDto getAuthorFromUsrAdmin = await _authorAppService.GetAsync(resultAuthorFromCreate.Id);

      UpdateAuthorDto updateAuthorDtoByUserAdmin = new()
      {
        Name = Consts.AdminEmail,
        BirthDate = getAuthorFromUsrAdmin.BirthDate,
        ShortBio = getAuthorFromUsrAdmin.ShortBio,
        ConcurrencyStamp = getAuthorFromUsrAdmin.ConcurrencyStamp
      };
      await _authorAppService.UpdateAsync(resultAuthorFromCreate.Id, updateAuthorDtoByUserAdmin);

      string concurrencyStampAdmin = default;
      DateTime? lastModificationTimeAdmin = default;
      Guid? lastModifierIdAdmin = default;

      await WithUnitOfWorkAsync(async () =>
      {
        Author authorEntityAfterUpdateUserAdmin = await _authorRepository.GetAsync(resultAuthorFromCreate.Id);

        concurrencyStampAdmin = authorEntityAfterUpdateUserAdmin.ConcurrencyStamp;

        authorEntityAfterUpdateUserAdmin.CreationTime.ShouldBe(createCreationTime);
        authorEntityAfterUpdateUserAdmin.CreatorId.ShouldBe(createCreatorId);
        concurrencyStampAdmin.ShouldNotBe(createConcurrencyStamp);

        lastModificationTimeAdmin = authorEntityAfterUpdateUserAdmin.LastModificationTime.Value;
        lastModificationTimeAdmin.ShouldNotBeNull();

        lastModifierIdAdmin = authorEntityAfterUpdateUserAdmin.LastModifierId.Value;
        lastModifierIdAdmin.ShouldNotBeNull();

      });

      ChangeCurrentPrincipal(Consts.User2ClaimUserId);

      System.Threading.Thread.Sleep(1000); 

      UpdateAuthorDto updateAuthorDtoByUser2 = new()
      {
        Name = Consts.User2Email,
        BirthDate = getAuthorFromUser2.BirthDate,
        ShortBio = getAuthorFromUser2.ShortBio,
        ConcurrencyStamp = getAuthorFromUser2.ConcurrencyStamp
      };

      _ = await Assert.ThrowsAnyAsync<AbpDbConcurrencyException>(async () =>
      {
        await _authorAppService.UpdateAsync(resultAuthorFromCreate.Id, updateAuthorDtoByUser2);
      });

      //await _authorAppService.UpdateAsync(resultAuthorFromCreate.Id, updateAuthorDtoByUser2);
      //// mit CreateUpdateAuthorDto werden alle Daten von UserAdmin überschrieben weil in CreateUpdateAuthorDto kein AenDtm und AenUsr gespeichert ist
      //await WithUnitOfWorkAsync(async () =>
      //{
      //  Author authorEntityAfterUpdateUser2 = await _authorRepository.GetAsync(resultAuthorFromCreate.Id);

      //  authorEntityAfterUpdateUser2.CreationTime.ShouldBe(createCreationTime);
      //  authorEntityAfterUpdateUser2.CreatorId.ShouldBe(createCreatorId);
      //  authorEntityAfterUpdateUser2.ConcurrencyStamp.ShouldNotBe(createConcurrencyStamp);
      //  authorEntityAfterUpdateUser2.ConcurrencyStamp.ShouldNotBe(concurrencyStampAdmin);
      //  authorEntityAfterUpdateUser2.LastModificationTime.Value.ShouldBeGreaterThanOrEqualTo(lastModificationTimeAdmin.Value);
      //  authorEntityAfterUpdateUser2.LastModificationTime.Value.ShouldNotBeNull();
      //  authorEntityAfterUpdateUser2.LastModifierId.Value.ShouldNotBe(lastModifierIdAdmin.Value);

      //  authorEntityAfterUpdateUser2.Name.ShouldBe(Consts.2Email);
      //  authorEntityAfterUpdateUser2.BirthDate.ShouldBe(getAuthorFromUser2.BirthDate);

      //});

    }

    [Fact]
    public async Task Repositore_Concurrency_Is_Ok()
    {
      AuthorDto resultAuthorFromCreate = await NewAuthorAsync();

      string createConcurrencyStamp = default;
      DateTime createCreationTime = default;
      DateTime? createLastModificationTime = default;
      Guid? createCreatorId = default;
      Guid? createLastModifierId = default;

      await WithUnitOfWorkAsync(async () =>
      {
        Author AuthorEntityFormCreate = await _authorRepository.GetAsync(resultAuthorFromCreate.Id);

        createConcurrencyStamp = AuthorEntityFormCreate.ConcurrencyStamp;
        createCreationTime = AuthorEntityFormCreate.CreationTime;
        createCreatorId = AuthorEntityFormCreate.CreatorId;
        createLastModificationTime = AuthorEntityFormCreate.LastModificationTime;
        createLastModifierId = AuthorEntityFormCreate.LastModifierId;

      });

      ChangeCurrentPrincipal(Consts.User2ClaimUserId);
      AuthorDto getAuthorFromUser2 = await _authorAppService.GetAsync(resultAuthorFromCreate.Id);

      ChangeCurrentPrincipal(Consts.AdminClaimUserId);
      AuthorDto getAuthorFromUsrAdmin = await _authorAppService.GetAsync(resultAuthorFromCreate.Id);

      System.Threading.Thread.Sleep(1000); 

      UpdateAuthorDto updateAuthorDtoByUserAdmin = new()
      {
        Name = Consts.AdminEmail,
        BirthDate = getAuthorFromUsrAdmin.BirthDate,
        ShortBio = getAuthorFromUsrAdmin.ShortBio,
        ConcurrencyStamp = getAuthorFromUsrAdmin.ConcurrencyStamp
      };

      await WithUnitOfWorkAsync(async () =>
      {
        Author authorEntityUserAdmin = await _authorRepository.GetAsync(resultAuthorFromCreate.Id);

        await _authorManager.ChangeNameAsync(authorEntityUserAdmin, updateAuthorDtoByUserAdmin.Name);
        authorEntityUserAdmin.BirthDate = updateAuthorDtoByUserAdmin.BirthDate;
        authorEntityUserAdmin.ShortBio = updateAuthorDtoByUserAdmin.ShortBio;
        authorEntityUserAdmin.ConcurrencyStamp = updateAuthorDtoByUserAdmin.ConcurrencyStamp;

        await _authorRepository.UpdateAsync(authorEntityUserAdmin, autoSave: true);
      });

      ChangeCurrentPrincipal(Consts.User2ClaimUserId);

      System.Threading.Thread.Sleep(1000); 

      UpdateAuthorDto updateAuthorDtoByUser2 = new()
      {
        Name = Consts.User2Email,
        BirthDate = getAuthorFromUser2.BirthDate,
        ShortBio = getAuthorFromUser2.ShortBio,
        ConcurrencyStamp = getAuthorFromUser2.ConcurrencyStamp
      };

      _ = await Assert.ThrowsAnyAsync<AbpDbConcurrencyException>(async () =>
      {
        await WithUnitOfWorkAsync(async () =>
        {
          Author authorEntityUserAdmin = await _authorRepository.GetAsync(resultAuthorFromCreate.Id);

          await _authorManager.ChangeNameAsync(authorEntityUserAdmin, updateAuthorDtoByUser2.Name);
          authorEntityUserAdmin.BirthDate = updateAuthorDtoByUser2.BirthDate;
          authorEntityUserAdmin.ShortBio = updateAuthorDtoByUser2.ShortBio;
          authorEntityUserAdmin.ConcurrencyStamp = updateAuthorDtoByUser2.ConcurrencyStamp;

          await _authorRepository.UpdateAsync(authorEntityUserAdmin, autoSave: true);
        });
      });

    }

    private async Task<AuthorDto> NewAuthorAsync()
    {

      DateTime now = DateTime.Now;
      DateTime beginTestTime = new(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
      string nameNeu = beginTestTime.ToString();

      AuthorDto resultAuthorFromCreate = await _authorAppService.CreateAsync(
          new CreateAuthorDto
          {
            Name = nameNeu,
            BirthDate = new DateTime(1850, 05, 22),
            ShortBio = "ShortBio"
          }
      );

      return resultAuthorFromCreate;
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
