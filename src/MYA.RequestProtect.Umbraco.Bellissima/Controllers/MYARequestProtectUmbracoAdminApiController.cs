using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MYA.RequestProtect.Options;
using Umbraco.Cms.Core.Security.Authorization;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Extensions;

namespace MYA.RequestProtect.Umbraco.Admin.Controllers
{
    [ApiVersion("1.0")]
    [ApiExplorerSettings(GroupName = "MYA.RequestProtect.Umbraco.Admin")]
    public class MYARequestProtectUmbracoAdminApiController : MYARequestProtectUmbracoAdminApiControllerBase
    {
        private readonly IOptionsMonitor<RequestProtectOptions> _optionsMonitor;
        private readonly IAuthorizationService _authorizationService;

        public MYARequestProtectUmbracoAdminApiController(IOptionsMonitor<RequestProtectOptions> optionsMonitor, IAuthorizationService authorizationService)
        {
            _optionsMonitor = optionsMonitor;
            _authorizationService = authorizationService;
        }

        [HttpGet("ping")]
        [ProducesResponseType<string>(StatusCodes.Status200OK)]
        public string Ping() => "Pong";

        [HttpGet("enbaled")]
        [ProducesResponseType<MyaRpEnabled>(StatusCodes.Status200OK)]
        public async Task<MyaRpEnabled> Enabled()
        {
            var config = _optionsMonitor.CurrentValue;

            AuthorizationResult authorizationResult = await _authorizationService.AuthorizeResourceAsync(
            User,
            UserGroupPermissionResource.WithKeys(Guid.Parse(Constants.GroupGuid)),
            AuthorizationPolicies.UserBelongsToUserGroupInRequest);

            var res = new MyaRpEnabled
            {
                Enabled = config.Enabled,
                Code = authorizationResult.Succeeded && config.Enabled ? $"{config.QueryKey}={config.Code}" : "---"
            };

            return res;
        }

        [HttpGet("protectrules")]
        [ProducesResponseType<AuthRules>(StatusCodes.Status200OK)]
        public async Task<AuthRules?> GetProtectRules()
        {
            var config = _optionsMonitor.CurrentValue;

            AuthorizationResult authorizationResult = await _authorizationService.AuthorizeResourceAsync(
            User,
            UserGroupPermissionResource.WithKeys(Guid.Parse(Constants.GroupGuid)),
            AuthorizationPolicies.UserBelongsToUserGroupInRequest);

            if (!authorizationResult.Succeeded)
            {
                return null;
            }

            return config.Rules;
        }

        public class MyaRpEnabled
        {
            public bool Enabled { get; set; } = false;
            public string? Code { get; set; }
        }
    }
}
