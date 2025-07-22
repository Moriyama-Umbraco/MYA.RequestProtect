export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "MYARequest Protect Umbraco Admin Dashboard",
    alias: "MYA.RequestProtect.Umbraco.Admin.Dashboard",
    type: "dashboard",
    js: () => import("./dashboard.element.js"),
    meta: {
      label: "Request Protect Dashboard",
      pathname: "mya-request-protect-dashboard",
    },
    conditions: [
      {
        alias: "Umb.Condition.SectionAlias",
        match: "Umb.Section.Settings",
      },
      {
        alias: "Umb.Condition.CurrentUser.GroupId",
        oneOf: ["a513b71b-0632-44be-b914-fdd07f7eeff1"]
      }
    ],
  },
];
