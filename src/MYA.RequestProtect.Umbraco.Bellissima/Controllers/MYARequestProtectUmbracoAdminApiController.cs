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
        private RequestProtectOptions _config;
        private IAuthorizationService _authorizationService;

        public MYARequestProtectUmbracoAdminApiController(IOptions<RequestProtectOptions> options, IAuthorizationService authorizationService)
        {
            _config = options.Value;
            _authorizationService = authorizationService;
        }

        [HttpGet("ping")]
        [ProducesResponseType<string>(StatusCodes.Status200OK)]
        public string Ping() => "Pong";

        [HttpGet("enbaled")]
        [ProducesResponseType<MyaRpEnabled>(StatusCodes.Status200OK)]
        public async Task<MyaRpEnabled> Enabled()
        {
            AuthorizationResult authorizationResult = await _authorizationService.AuthorizeResourceAsync(
            User,
            UserGroupPermissionResource.WithKeys(Guid.Parse(Constants.GroupGuid)),
            AuthorizationPolicies.UserBelongsToUserGroupInRequest);

            var res = new MyaRpEnabled
            {
                Enabled = _config.Enabled,
                Code = authorizationResult.Succeeded && _config.Enabled ? $"{_config.QueryKey}={_config.Code}" : "---"
            };

            return res;
        }

        [HttpGet("protectrules")]
        [ProducesResponseType<AuthRules>(StatusCodes.Status200OK)]
        public async Task<AuthRules?> GetProtectRules()
        {
            AuthorizationResult authorizationResult = await _authorizationService.AuthorizeResourceAsync(
            User,
            UserGroupPermissionResource.WithKeys(Guid.Parse(Constants.GroupGuid)),
            AuthorizationPolicies.UserBelongsToUserGroupInRequest);

            if (!authorizationResult.Succeeded)
            {
                return null;
            }

            return _config.Rules;
        }

        public class MyaRpEnabled
        {
            public bool Enabled { get; set; } = false;
            public string? Code { get; set; }
        }
    }
}
