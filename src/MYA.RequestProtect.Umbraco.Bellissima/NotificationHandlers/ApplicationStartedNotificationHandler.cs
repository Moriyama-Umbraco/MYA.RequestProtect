using Microsoft.Extensions.Logging;
using MYA.RequestProtect.Umbraco.Admin;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using UmbCore = Umbraco.Cms.Core;

namespace MYA.RequestProtect.Umbraco.Bellissima.NotificationHandlers;

public class ApplicationStartedNotificationHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly ILogger<ApplicationStartedNotificationHandler> _logger;
    private readonly IUserGroupService _userGroupService;
    private readonly IShortStringHelper _shortStringHelper;

    public ApplicationStartedNotificationHandler(ILogger<ApplicationStartedNotificationHandler> logger, 
        IUserGroupService userGroupService, IShortStringHelper shortStringHelper)
    {
        _logger = logger;
        _userGroupService = userGroupService;
        _shortStringHelper = shortStringHelper;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification _, CancellationToken _1)
    {
        try
        {
            if (await _userGroupService.GetAsync(Constants.GroupAlias) is null)
            {
                var res = await _userGroupService.CreateAsync(new UserGroup(_shortStringHelper)
                {
                    Alias = Constants.GroupAlias,
                    Key = Guid.Parse(Constants.GroupGuid),
                    Name = Constants.GroupName,
                    Icon = "icon-shield",
                }, UmbCore.Constants.Security.SuperUserKey);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to create/verify Request Protect user group");
        }
        
    }

}