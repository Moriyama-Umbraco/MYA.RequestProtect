export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "MYARequest Protect Umbraco Admin Entrypoint",
    alias: "MYA.RequestProtect.Umbraco.Admin.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint.js"),
  },
];
