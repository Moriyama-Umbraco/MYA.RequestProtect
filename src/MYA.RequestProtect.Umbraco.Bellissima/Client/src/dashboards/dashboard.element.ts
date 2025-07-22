import {
  LitElement,
  css,
  html,
  customElement,
  state
} from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { AuthRules, MyaRequestProtectUmbracoAdminService } from "../api";

@customElement("mya-request-protect-dashboard")
export class MYARequestProtectDashboardElement extends UmbElementMixin(LitElement) {
  
  @state()
  private _currentRules: AuthRules | undefined;
  private _myaEnabled: boolean = false;
  private _authCode: string | null | undefined;

  constructor() {
    super();

    MyaRequestProtectUmbracoAdminService.enabled().then((response) => {
      if (response.data && response.data.enabled) {
        this._myaEnabled = true;
        this._authCode = response.data.code;

        MyaRequestProtectUmbracoAdminService.getProtectRules().then(rules => {
          this._currentRules = rules.data;
        });
      }
    });
    
    
  }

  render() {
    return html`
      <uui-box headline="Configuration" class="wide">
        <p>Status: <b>${this._myaEnabled ? "Enabled" : "Disabled"}</b></p>
        <p>Auth Code: <b>?${this._authCode}</b> </p>
        <p>Rules:</p>
        <umb-code-block language="json" copy
          >${JSON.stringify(this._currentRules, null, 2)}</umb-code-block
        >
      </uui-box>
    `;
  }

  static styles = [
    css`
      :host {
        display: grid;
        gap: var(--uui-size-layout-1);
        padding: var(--uui-size-layout-1);
        grid-template-columns: 1fr 1fr 1fr;
      }

      uui-box {
        margin-bottom: var(--uui-size-layout-1);
      }

      h2 {
        margin-top: 0;
      }

      .wide {
        grid-column: span 3;
      }
    `,
  ];
}

export default MYARequestProtectDashboardElement;

declare global {
  interface HTMLElementTagNameMap {
    "mya-request-protect-dashboard": MYARequestProtectDashboardElement;
  }
}
